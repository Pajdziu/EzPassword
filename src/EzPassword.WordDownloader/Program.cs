﻿namespace EzPassword.WordDownloader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using Core.Config;
    using Core.Wiki;
    using Newtonsoft.Json;
    using NLog;

    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            ParserResult<CommandLineOptions> parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            parserResult.WithParsed(options =>
            {
                IDictionary<string, Language> wikiConfig = ReadWikiConfig();
                Language language = wikiConfig[options.LanguageSymbol];

                IEnumerable<Task> tasks = RunTasks(language, options.OutDirectory);

                Task.WhenAll(tasks).Wait();

                Console.WriteLine("Completed!");
            });
        }

        private static IEnumerable<Task> RunTasks(Language language, string outDirectory)
        {
            Task nounTask = Task.Run(() =>
            {
                IObservable<string> nounGenerator = new WikiWordDownloader(language.NounCategory);
                var nounSubscriber = new TextFileWordPersister(
                    $@"{outDirectory}\{language.Symbol}\{WordDirectoryConfig.NounDirectoryName}",
                    WordDirectoryConfig.NounFileNameTemplate);
                Logger.Info("Saving nouns starting with letters: ");
                nounGenerator.Subscribe(nounSubscriber);
                nounGenerator.Wait();
            });
            yield return nounTask;

            Task adjectiveTask = Task.Run(() =>
            {
                IObservable<string> adjectiveGenerator = new WikiWordDownloader(language.AdjectiveCategory);
                var adjectiveSubscriber = new TextFileWordPersister(
                    $@"{outDirectory}\{language.Symbol}\{WordDirectoryConfig.AdjectiveDirectoryName}",
                    WordDirectoryConfig.AdjectiveFileNameTemplate);
                Logger.Info("Saving adjectives starting with letters: ");
                adjectiveGenerator.Subscribe(adjectiveSubscriber);
                adjectiveGenerator.Wait();
            });
            yield return adjectiveTask;
        }

        private static IDictionary<string, Language> ReadWikiConfig()
        {
            const string WikiConfigFileName = "WikiConfig.json";
            var wikiConfig = File.ReadAllText(WikiConfigFileName);
            var languages = JsonConvert.DeserializeObject<List<Language>>(wikiConfig);
            return languages.ToDictionary(lang => lang.Symbol, lang => lang);
        }
    }
}