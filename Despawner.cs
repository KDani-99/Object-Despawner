using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Despawner
{
    abstract class Destructable
    {
        private static HashSet<Type> typleTypes = new HashSet<Type>() {
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
            typeof(Tuple<,,,,,,,>),
         };
        public static void Destruct(object obj)
        {
            try
            {
                /* Get Tuple fields in the class and check for PoolObject */
                var fieldsWithTuples = obj
                .GetType()
                .GetFields(BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.FlattenHierarchy)
                .Where(field => field.FieldType.IsGenericType)
                .Where(field => typleTypes.Contains(field.FieldType.GetGenericTypeDefinition()))
                .Select(field => new {
                    name = field.Name,
                    value = field.GetValue(field.IsStatic
                            ? null                     // we should provide null for static
                            : obj)
                })
                .Where(item => item.value != null);

                foreach (var tuple in fieldsWithTuples.Select(f => f.value))
                {
                    DestructTuples(tuple);
                }

                /* Get Tuple properties in the class and check for PoolObject */
                var propertiesWithTuples = obj
                .GetType()
                .GetProperties(BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.FlattenHierarchy)
                .Where(prop => prop.PropertyType.IsGenericType)
                .Where(prop => typleTypes.Contains(prop.PropertyType.GetGenericTypeDefinition()))
                .Select(prop => new {
                    name = prop.Name,
                    value = prop.GetValue(obj)
                })
                .Where(item => item.value != null);

                foreach (var tuple in propertiesWithTuples.Select(f => f.value))
                {
                    DestructTuples(tuple);
                }

                FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                /* Get every PoolObject field in the class */
                FieldInfo[] poolObjectFields = fields.Where(field => field.FieldType.IsSubclassOf(typeof(PoolObject))).ToArray();

                foreach (FieldInfo field in poolObjectFields)
                {
                    PoolObject poolObject = (PoolObject)field.GetValue(obj);
                    /* If the object exists and not null (obviously), then destroy it */
                    if (poolObject != null && poolObject.Exists())
                    {
                        poolObject.Delete();
                        field.SetValue(obj, null);
                    }
                }

                PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                /* Get every PoolObject property in the class */
                PropertyInfo[] poolObjectProps = props.Where(prop => prop.PropertyType.IsSubclassOf(typeof(PoolObject))).ToArray();

                foreach (PropertyInfo prop in poolObjectProps)
                {

                    PoolObject poolObject = (PoolObject)prop.GetValue(obj, null);
                    /* If the object exists and not null (obviously), then destroy it */
                    if (poolObject != null && poolObject.Exists())
                    {
                        poolObject.Delete();
                        prop.SetValue(obj, null, null);
                    }
                }

                /* Check for fields */
                FieldInfo[] poolCollectionFields = fields.Where(field => typeof(ICollection).IsAssignableFrom(field.FieldType) || typeof(IEnumerable).IsAssignableFrom(field.FieldType)).ToArray();
                foreach (var elem in poolCollectionFields)
                {
                    if (typeof(IDictionary).IsAssignableFrom(elem.FieldType))
                    {
                        IDictionary dictionary = (IDictionary)elem.GetValue(obj);
                        if (dictionary != null)
                            DestructDictionaries(dictionary);
                    }
                    else
                    {
                        IEnumerable collection = (IEnumerable)elem.GetValue(obj);
                        if (collection != null)
                            DestructNested(collection);

                    }
                    elem.SetValue(obj, null);
                }

                /* Check for properties */
                PropertyInfo[] poolCollectionProps = props.Where(prop => typeof(ICollection).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)).ToArray();
                foreach (var elem in poolCollectionProps)
                {
                    if (typeof(IDictionary).IsAssignableFrom(elem.PropertyType))
                    {
                        IDictionary dictionary = (IDictionary)elem.GetValue(obj, null);
                        if (dictionary != null)
                            DestructDictionaries(dictionary);
                    }
                    else
                    {
                        IEnumerable collection = (IEnumerable)elem.GetValue(obj, null);
                        if (collection != null)
                            DestructNested(collection);

                    }
                    elem.SetValue(obj, null, null);
                }
               
            }
            catch (Exception ex)
            {
                // Throw Exception
                Debug.WriteLine("An error has occured in the Destruct() method, objects were not deleted " + ex.ToString());
            }
        }
        private static void DestructNested(IEnumerable collection)
        {
            foreach (var elem in collection)
            {
                if (elem.GetType().IsSubclassOf(typeof(PoolObject)))
                {
                    // If it is a PooObject, delete it
                    PoolObject poolObject = (PoolObject)elem;
                    if (poolObject != null && poolObject.Exists())
                    {
                        poolObject.Delete();
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(elem.GetType()))
                {
                    // If it is a Dictionary, call the DestructDictionaries method
                    DestructDictionaries((IDictionary)elem);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(elem.GetType()))
                {
                    DestructNested((IEnumerable)elem);
                }
                else if (elem.GetType().IsGenericType && typleTypes.Contains(elem.GetType().GetGenericTypeDefinition()))
                {
                    // If it is a Tuple, call the DestructTuples method
                    DestructTuples(elem);
                }
            }
        }
        private static void DestructTuples(object tuple)
        {
            foreach (var prop in tuple.GetType().GetProperties())
            {
                var value = tuple.GetType().GetProperty(prop.Name).GetValue(tuple, null);
                if (value != null)
                {
                    if (value.GetType().IsSubclassOf(typeof(PoolObject)))
                    {
                        // If it is a PooObject, delete it
                        PoolObject poolObject = (PoolObject)value;
                        if (poolObject != null && poolObject.Exists())
                            poolObject.Delete();
                    }
                    if(typeof(IDictionary).IsAssignableFrom(value.GetType()))
                    {
                        // If it is a Dictionary, call the DestructDictionaries method
                        DestructDictionaries((IDictionary)value);
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(value.GetType()))
                    {
                        // If it is an IEnumerable, call the DestructNested method
                        DestructNested((IEnumerable)value);
                    }
                }
            }
        }
        /* Destruct dictionaries */
        private static void DestructDictionaries(IDictionary dictionary)
        {
            if (dictionary != null)
            {
                IEnumerable keys = dictionary.Keys;
                IEnumerable values = dictionary.Values;

                if (keys != null)
                    DestructNested(keys);
                if (values != null)
                    DestructNested(values);
            }
        }
    }
}