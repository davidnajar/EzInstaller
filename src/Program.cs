using CommandLine;
using System;

namespace EzInstaller
{
    class Program
    {
        static void Main(string[] args)
        {

             CommandLine.Parser.Default.ParseArguments<CreateOptions, UpdateOptions>(args)
                .MapResult(
                    (CreateOptions opts) => RunPack(opts),
                    (UpdateOptions opts) => RunUpdate(opts),
                       errs => 1);

        }

       static int RunPack(CreateOptions opts)
        {
            Console.WriteLine("Generating installer");
            using (var packer = new Packer())

            {
                packer.AddFolder(opts.Folder, true);
                packer.CompileArchive(opts.OutputFileName);
            }

                return 0;
        }

        static int RunUpdate(UpdateOptions opts)
        {


            return 0;
        }



        [Verb("pack", HelpText = "Creates an installer with options")]
        class CreateOptions
        {
            //normal options here
            [Option('f', "folder", Required = true, HelpText = "Input folder to be packed.")]
            public string Folder  { get; set; }

            // Omitting long name, defaults to name of property, ie "--verbose"
            [Option('o', "output", Required = true, HelpText = "Output file")]
            public string OutputFileName { get; set; }

           
        }

        [Verb("update", HelpText = "Updates the tool")]
        class UpdateOptions
        {
            //commit options here
        }
    }
}
