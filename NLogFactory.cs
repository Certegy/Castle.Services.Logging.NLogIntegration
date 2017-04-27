// Copyright 2004-2012 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Services.Logging.NLogIntegration
{
	using System;
    using System.Collections.Generic;
	using System.Reflection;
    using System.Reflection.Emit;

    using Castle.Core.Logging;

	using NLog;
	using NLog.Config;

	/// <summary>
	///   Implementation of <see cref="ILoggerFactory" /> for NLog.
	/// </summary>
	public class NLogFactory : AbstractLoggerFactory
	{
		internal const string defaultConfigFileName = "nlog.config";

        private readonly MappedDiagnosticsLogicalContextDelegate logicalThreadDictionary;

        /// <summary>
        ///   Initializes a new instance of the <see cref="NLogFactory" /> class.
        /// </summary>
        public NLogFactory()
			: this(defaultConfigFileName)
		{
            /* 
                The following creates an easy way to get at the private static LogicalThreadDictionary of NLog's MappedDiagnosticsLogicalContext.
                It does some IL emitting, so it's not the most maintainable, but it's a once only cost at applicaiton startup.
                There are probably other ways to achieve the same thing, but none as performant.
            */
            var type1 = typeof(MappedDiagnosticsContext);
            var getLogicalThreadDictionary1 = type1.GetMethod("GetLogicalThreadDictionary", BindingFlags.Static | BindingFlags.NonPublic);

            var method1 = new DynamicMethod("GetMappedDiagnosticsLogicalContext", typeof(IDictionary<string, object>), null, typeof(MappedDiagnosticsLogicalContext), true);
            var ilGen1 = method1.GetILGenerator();
            ilGen1.Emit(OpCodes.Call, getLogicalThreadDictionary1);
            ilGen1.Emit(OpCodes.Ret);

            logicalThreadDictionary = (MappedDiagnosticsLogicalContextDelegate)method1.CreateDelegate(typeof(MappedDiagnosticsLogicalContextDelegate));
        }

		/// <summary>
		///   Initializes a new instance of the <see cref="NLogFactory" /> class.
		/// </summary>
		/// <param name="configuredExternally">If <c>true</c>. Skips the initialization of log4net assuming it will happen externally. Useful if you're using another framework that wants to take over configuration of NLog.</param>
		public NLogFactory(bool configuredExternally)
		{
			if (configuredExternally)
			{
				return;
			}

			var file = GetConfigFile(defaultConfigFileName);
			LogManager.Configuration = new XmlLoggingConfiguration(file.FullName);
		}

		/// <summary>
		///   Initializes a new instance of the <see cref="NLogFactory" /> class.
		/// </summary>
		/// <param name="configFile"> The config file. </param>
		public NLogFactory(string configFile)
		{
			var file = GetConfigFile(configFile);
			LogManager.Configuration = new XmlLoggingConfiguration(file.FullName);
		}

		/// <summary>
		///   Initializes a new instance of the <see cref="NLogFactory" /> class.
		/// </summary>
		/// <param name="loggingConfiguration"> The NLog Configuration </param>
		public NLogFactory(LoggingConfiguration loggingConfiguration)
		{
			LogManager.Configuration = loggingConfiguration;
		}

		/// <summary>
		///   Creates a logger with specified <paramref name="name" />.
		/// </summary>
		/// <param name="name"> The name. </param>
		/// <returns> </returns>
		public override Core.Logging.ILogger Create(String name)
		{
			var log = LogManager.GetLogger(name);
            return new NLogLogger(log, this, logicalThreadDictionary);
        }

		/// <summary>
		///   Not implemented, NLog logger levels cannot be set at runtime.
		/// </summary>
		/// <param name="name"> The name. </param>
		/// <param name="level"> The level. </param>
		/// <returns> </returns>
		/// <exception cref="NotImplementedException" />
		public override Core.Logging.ILogger Create(String name, LoggerLevel level)
		{
			throw new NotSupportedException("Logger levels cannot be set at runtime. Please review your configuration file.");
		}
	}
}