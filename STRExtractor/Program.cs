using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STRExtractor
{
    public class Faixa
    {
        public string Id { get; set; }
        public string UID { get; set; }
        public string TipoFaixa { get; set; }
        public string Idioma { get; set; }
    }

    class Program
    {
        private static string mkvextract = @"C:\Program Files\MKVToolNix\mkvextract.exe";
        private static string mkvinfo = @"C:\Program Files\MKVToolNix\mkvinfo.exe";
        static List<Faixa> faixas = new List<Faixa>();
        private static StringBuilder output = new StringBuilder();

        static void Main(string[] args)
        {
            //Define o output como utf8
            Console.OutputEncoding = Encoding.UTF8;

            #region Verifica se o mkvtools está instalado
            if (!File.Exists(mkvextract))
            {
                Console.WriteLine($"Não foi possível encontrar o arquivo {mkvextract}, por favor instale o mkvtoolnix x64 em sua máquina. https://mkvtoolnix.download/downloads.html#windows");
            }

            if (!File.Exists(mkvinfo))
            {
                Console.WriteLine($"Não foi possível encontrar o arquivo {mkvinfo}, por favor instale o mkvtoolnix x64 em sua máquina. https://mkvtoolnix.download/downloads.html#windows");
            }
            #endregion

            //Obtém diretório a ser rastreado:
            Console.WriteLine("Digite a pasta onde estão seus arquivos MKV:");
            string pasta = Console.ReadLine();

            if (System.IO.Directory.Exists(pasta))
            {
                //Obtém lista de arquivos mkv
                List<string> arquivos = ListarArquivos(pasta);

                //Extrai subs dos arquivos
                foreach (var item in arquivos)
                {
                    Console.WriteLine($"\r\n\r\n Inicio {item}\r\n\r\n");
                    LerArquivo(item);
                    faixas = new List<Faixa>();
                    Console.WriteLine($"\r\n\r\n Fim da leitura do arquivo {item}\r\n\r\n");
                }
            }
            else
            {
                Console.WriteLine("A pasta digitada não existe");
            }
            Console.WriteLine("\r\n\r\n\r\n\r\nFim do Programa");
            Console.ReadLine();
        }

        /// <summary>
        /// Faz busca dos arquivos mkv nos diretórios e subdiretórios.
        /// </summary>
        /// <param name="diretorio"></param>
        /// <returns>Lista de diretórios</returns>
        public static List<string> ListarArquivos(string diretorio)
        {
            List<String> files = new List<String>();
            files.AddRange(Directory.GetFiles(diretorio, "*.mkv", SearchOption.AllDirectories));
            return files;
        }

        /// <summary>
        /// Obtém faixas contendo subtitulos dentro do arquivo mkv
        /// </summary>
        /// <param name="arquivo">Endereço do arquivo mkv</param>
        public static void LerArquivo(string arquivo)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            string args = $" --no-gui \"{arquivo}\"";
            bool insideFaixa = true;
            bool fimFaixa = true;

            Faixa fx = new Faixa();
            process.StartInfo.FileName = mkvinfo;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    if (fimFaixa)
                    {
                        insideFaixa = e.Data.Contains("+ Uma faixa");
                    }
                    fimFaixa = e.Data.Contains("Canais") || e.Data.Contains("Codec privado");
                    if (insideFaixa && !fimFaixa)
                    {
                        if (e.Data.Contains("ID da faixa"))
                        {
                            fx.Id = e.Data.Remove(0, e.Data.IndexOf("mkvextract: ")).Replace("mkvextract: ", "").Replace(")", "");
                        }
                        if (e.Data.Contains("Tipo"))
                        {
                            fx.TipoFaixa = e.Data.Remove(0, e.Data.IndexOf("xa: ")).Replace("xa: ", "");
                        }
                        if (e.Data.Contains("Idioma"))
                        {
                            fx.Idioma = e.Data.Remove(0, e.Data.IndexOf("Idioma: ")).Replace("Idioma: ", "");
                        }
                    }
                    if (fimFaixa)
                    {
                        faixas.Add(fx);
                        fx = new Faixa();
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.Close();

            //Executa Extração das faixas de subtitulos
            ExtrairSubs(arquivo, faixas);
        }

        /// <summary>
        /// Extrai e salva arquivos de subtitulos
        /// </summary>
        /// <param name="arquivo">Endereço do arquivo mkv</param>
        /// <param name="faixas">Faixas do arquivo</param>
        public static void ExtrairSubs(string arquivo, List<Faixa> faixas)
        {
            Process process = new Process();
            
            int lineCount = 0;
            string nomearquivo = Path.GetFileNameWithoutExtension(arquivo);
            string pasta = Path.GetDirectoryName(arquivo);

            string args = $" --ui-language en tracks \"{arquivo}\" ";
            foreach (var item in faixas.Where(f => f.TipoFaixa == "subtitles"))
            {
                args += $"{item.Id}:\"{pasta}\\{nomearquivo}_track{item.Id}_{item.Idioma}.idx\" ";
            }
            
            process.StartInfo.FileName = mkvextract;
            process.StartInfo.Arguments = args;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Exited += (sender, e) => { Console.WriteLine("Fim"); };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.Contains("%"))
                    {
                        string texto = $"{nomearquivo}: {e.Data}";
                        Console.Write(texto);
                        Console.SetCursorPosition(0, lineCount);
                    }
                    else
                    {
                        lineCount = Console.CursorTop;
                    }
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.Close();

        }
    }
}
