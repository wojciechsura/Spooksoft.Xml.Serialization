using Spooksoft.Xml.Serialization.Models;

namespace Spooksoft.Xml.Serialization.Infrastructure
{
    internal class ClassInfoProvider : IClassSerializationInfoProvider
    {
        private readonly Dictionary<Type, BaseClassInfo> typeCache;
        private readonly object typeCacheLock;

        public ClassInfoProvider(Dictionary<Type, BaseClassInfo> typeCache, object typeCacheLock)
        {
            this.typeCache = typeCache;
            this.typeCacheLock = typeCacheLock;
        }

        public BaseClassInfo GetClassInfo(Type type)
        {
            BaseClassInfo? classInfo;

            lock (typeCacheLock)
            {
                if (!typeCache.TryGetValue(type, out classInfo))
                {
                    classInfo = ClassInfoBuilder.BuildClassInfo(type);
                    typeCache[type] = classInfo;
                }
            }

            return classInfo!;
        }
    }
}
