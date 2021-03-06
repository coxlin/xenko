// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;

namespace SiliconStudio.AssemblyProcessor
{
    internal class RoslynExtraCodeProcessor : IAssemblyDefinitionProcessor
    {
        public string SignKeyFile { get; private set; }

        public List<string> References { get; private set; }

        public List<AssemblyDefinition> MemoryReferences { get; private set; }

        public TextWriter Log { get; private set; }

        public List<RoslynCodeMerger.SourceCode> SourceCodes { get; } = new List<RoslynCodeMerger.SourceCode>();

        public RoslynExtraCodeProcessor(string signKeyFile, List<string> references, List<AssemblyDefinition> memoryReferences, TextWriter log)
        {
            SignKeyFile = signKeyFile;
            References = references;
            MemoryReferences = memoryReferences;
            Log = log;
        }

        public void RegisterSourceCode(string sourceCode, string name = null)
        {
            SourceCodes.Add(new RoslynCodeMerger.SourceCode { Code = sourceCode, Name = name });
        }

        public bool Process(AssemblyProcessorContext context)
        {
            if (SourceCodes.Count == 0)
                return false;

            // Generate serialization assembly
            var serializationAssemblyFilepath = RoslynCodeMerger.GenerateRolsynAssemblyLocation(context.Assembly.MainModule.FileName);
            context.Assembly = RoslynCodeMerger.GenerateRoslynAssembly(context.AssemblyResolver, context.Assembly, serializationAssemblyFilepath, SignKeyFile, References, MemoryReferences, Log, SourceCodes);

            return true;
        }
    }
}
