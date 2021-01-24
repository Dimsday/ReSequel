﻿using System;
using System.IO;
using Extension.Cache;
using Extension.ConfigurationRelated;
using Extension.ExtensionStatus;
using Extension.ExtensionStatus.FullyLoaded;
using Extension.Tagging.Extractor;
using Main.Helper;
using Main.Inclusion.Scanner;
using Main.Logger;
using Main.Other;
using Main.ScanRelated;
using Main.SolutionValidator;
using Main.Sql.ConnectionString;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Ninject;
using Ninject.Modules;

namespace Extension.CompositionRoot.Modules
{
    public class DefaultModule : NinjectModule
    {
        private readonly string _pathToXmlConfigurationFile;


        internal EnvDTE.DTE _dte; //do not remove! https://social.msdn.microsoft.com/Forums/en-US/eb6cc3eb-422a-48b1-86da-7a81d3edbddc/events-not-captured-afte-a-window-is-opened?forum=vsx    Your solution events aren't firing because the objects are getting collected



        public DefaultModule(
            string pathToXmlConfigurationFile
            )
        {
            if (pathToXmlConfigurationFile == null)
            {
                throw new ArgumentNullException(nameof(pathToXmlConfigurationFile));
            }

            _pathToXmlConfigurationFile = pathToXmlConfigurationFile;
        }

        public override void Load()
        {
            Bind<ILastMessageProcessLogger, IProcessLogger>()
                .To<LastMessageProcessLogger>()
                .InSingletonScope()
                .WithConstructorArgument(
                    "discret",
                    25
                    )
                ;

            Bind<ConfigurationFilePath>()
                .ToSelf()
                .InSingletonScope()
                .WithConstructorArgument(
                    "filePath",
                    _pathToXmlConfigurationFile
                    )
                ;

            Bind<IConfigurationProvider, IConnectionStringContainer>()
                .To<ConfigurationProvider>()
                .InSingletonScope()
                ;

            Bind<Scan>()
                .ToMethod(
                    c =>
                    {
                        var solutionNameProvider = c.Kernel.Get<ISolutionNameProvider>();

                        if (string.IsNullOrWhiteSpace(solutionNameProvider.SolutionName))
                        {
                            //no open solution found, so return empty Scan
                            return new Scan();
                        }

                        var configurationProvider = c.Kernel.Get<IConfigurationProvider>();
                        if (!configurationProvider.TryRead(out _))
                        {
                            throw new InvalidOperationException("Cannot read configuration file");
                        }

                        var solutionNamePath = new FileInfo(solutionNameProvider.SolutionName);
                        var solutionFolder = solutionNamePath.Directory.FullName;
                        var scanFilePath = Path.Combine(solutionFolder, Root.ScanSchemeFileName);

                        if (!File.Exists(scanFilePath))
                        {
                            //if scan file does not exists for this solution, we create it with default one
                            Root.ExtractEmbeddedResource(scanFilePath, "Extension." + Root.ScanSchemeFileName);
                        }

                        var scan = scanFilePath.ReadXml<Scan>();

                        return
                            scan;
                    })
                .InTransientScope()
                ;


            Bind<IInclusionScannerFactory>()
                .To<InclusionScannerFactory>()
                .InSingletonScope()
                .WithConstructorArgument(
                    "scanFunc",
                    c => new Func<Scan>(() => c.Kernel.Get<Scan>())
                    )
                ;

            Bind<ISolutionValidatorFactory>()
                .To<DefaultSolutionValidatorFactory>()
                .InSingletonScope()
                ;

            Bind<ISolutionValidator>()
                .To<DefaultSolutionValidator>()
                //.InSingletonScope()
                //not a singleton
                ;

            Bind<IVsSolutionEventsExt, SqlInclusionCache>()
                .To<SqlInclusionCache>()
                .InSingletonScope()
                ;

            Bind<IVsSolutionEventsExt, ITimeoutTagExtractor, ITagExtractor>()
                .To<TimeoutTagExtractor>()
                .InSingletonScope()
                .WithConstructorArgument(
                    "timeout",
                    TimeSpan.FromSeconds(3)
                    )
                ;






            ThreadHelper.ThrowIfNotOnUIThread(nameof(DefaultModule));

            var solution = AsyncPackage.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            Bind<IVsSolution>()
                .ToConstant(solution)
                .InSingletonScope()
                ;


            Bind<ISolutionNameProvider, IExtensionStatus, IVsSolutionEventsExt>()
                .To<ExtensionStatusContainer>()
                .InSingletonScope()
                ;


            //_kernel
            //    .Bind<MessageBox>()
            //    .ToSelf()
            //    .InSingletonScope()
            //    ;

            _dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            Bind<EnvDTE.DTE>()
                .ToConstant(_dte)
                .InSingletonScope()
                ;

            Bind<IFullyLoadedStatusContainer, IFullyLoadedStatusProvider>()
                .To<FullyLoadedStatusContainer>()
                .InSingletonScope()
                ;
        }
    }
}
