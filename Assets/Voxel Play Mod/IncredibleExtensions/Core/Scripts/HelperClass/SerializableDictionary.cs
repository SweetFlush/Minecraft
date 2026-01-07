using System;
using System.Collections.Generic;

namespace IncredibleExtensions.VPAddons{
    [Serializable]
    public class SerializableDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        // Constructor to populate from a Dictionary
        public SerializableDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        // Default constructor for deserialization
        public SerializableDictionary() { }

        // Method to convert back to a Dictionary
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dictionary[keys[i]] = values[i];
            }
            return dictionary;
        }
}
}