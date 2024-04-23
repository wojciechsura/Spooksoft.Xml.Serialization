using Spooksoft.Xml.Serialization.Infrastructure;
using Spooksoft.Xml.Serialization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializer
    {
        private static readonly Dictionary<Type, BaseClassInfo> sharedTypeCache = new();
        private static readonly object sharedTypeCacheLock = new();

        private readonly Dictionary<Type, BaseClassInfo> typeCache;
        private readonly object typeCacheLock;
                
        private BaseClassInfo EnsureClassInfo(Type type)
        {
            BaseClassInfo? classInfo;

            lock(typeCacheLock)
            {
                if (typeCache.TryGetValue(type, out classInfo))
                {
                    classInfo = ClassInfoBuilder.BuildClassInfo(type);
                    typeCache[type] = classInfo;
                }
            }

            return classInfo!;            
        }

        public XmlSerializer(XmlSerializerConfig? config = null)
        {
            config ??= new XmlSerializerConfig();

            if (config.UseSharedTypeCache)
            {
                typeCache = sharedTypeCache;
                typeCacheLock = sharedTypeCacheLock;
            }
            else
            {
                typeCache = new();
                typeCacheLock = new();
            }
        }

        public void Serialize(object model, Stream s)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(Stream s)
        {
            throw new NotImplementedException();
        }
    }
}
