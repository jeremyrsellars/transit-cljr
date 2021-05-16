// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
//
// This code is a C# port of the Java version created and maintained by Cognitect, therefore
//
// Copyright © 2014 Cognitect. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Sellars.Transit.Alpha;
using Sellars.Transit.Impl;

namespace Beerendonk.Transit.Impl
{
    /// <summary>
    /// Implements a writer factory.
    /// </summary>
    internal partial class WriterFactory
    {
        public static IWriter<T> GetMsgPackInstance<T>(Stream output, IDictionary<Type, IWriteHandler> customHandlers)
        {
            IImmutableDictionary<Type, IWriteHandler> handlers = Handlers(customHandlers);
            MessagePackEmitter emitter = new MessagePackEmitter(output, handlers);

            SetSubHandler(handlers, emitter);
            WriteCache wc = new WriteCache();

            return new MsgPackWriter<T>(output, emitter, wc);
        }

        private class MsgPackWriter<T> : IWriter<T>
        {
            private Stream output; 
            private MessagePackEmitter emitter;
            private WriteCache wc;

            public MsgPackWriter(Stream output, MessagePackEmitter emitter, WriteCache wc)
            {
                this.output = output;
                this.emitter = emitter;
                this.wc = wc;
            }

            public void Write(T value)
            {
                emitter.Emit(value, false, wc.Init());
                output.Flush();
            }
        }
    }
}
