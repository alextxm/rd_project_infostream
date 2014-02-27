using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nest.Extensions
{
    public class TypesStore
    {
        public Type Type { get; set; }
        public string TypeName { get; set; }
    }

    public class TypesStoreManager
    {
        private List<TypesStore> typesStore = new List<TypesStore>();

        public string InferTypeName(Type t)
        {
            TypesStore ts = typesStore.FirstOrDefault(p => p.Type == t);
            if (ts == null)
            {
                AddType(t);
                ts = typesStore.First(p => p.Type == t);
            }

            return ts.TypeName;
        }

        public bool AddType(Type t, string typeName = null, bool forceOverride = false)
        {
            string name = String.Empty;

            if (String.IsNullOrEmpty(typeName))
                name = Nest.Resolvers.Inflector.MakePlural(t.Name).ToLower();
            else
                name = typeName;

            TypesStore ts = typesStore.FirstOrDefault(p => p.Type == t);
            if (ts == null)
                typesStore.Add(new TypesStore() { Type = t, TypeName = name });
            else
            {
                if (forceOverride)
                    ts.TypeName = name;
                else
                    return false;
            }

            return true;
        }

        public bool DeleteType(Type t)
        {
            TypesStore ts = typesStore.FirstOrDefault(p => p.Type == t);
            if (ts != null)
                typesStore.Remove(ts);
            else
                return false;

            return true;
        }

        public bool Clear()
        {
            typesStore.Clear();
            return true;
        }
    }
}
