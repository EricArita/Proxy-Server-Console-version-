using System;
using System.Collections.Generic;

namespace TestProxyOnConsole
{
    public class VDictionary : IDisposable //String only
    {

        //IDisposable Implementation

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                kvp.Clear();
                kvp = null;
            }

            disposed = true;
        }

        List<KeyValuePair<string, string>> kvp = new List<KeyValuePair<string, string>>();

        public IEnumerable<KeyValuePair<string, string>> Items
        {
            get
            {
                foreach (KeyValuePair<string, string> lvp in kvp)
                {
                    yield return lvp;
                }
            }
        }

        public int Count
        {
            get { return kvp.Count; }
        }

        public List<string> Keys
        {
            get
            {
                List<string> temp = new List<string>();

                foreach (KeyValuePair<string, string> lvp in kvp)
                {
                    temp.Add(lvp.Key);
                }

                return temp;
            }
        }

        public List<string> Values
        {
            get
            {
                List<string> temp = new List<string>();

                foreach (KeyValuePair<string, string> lvp in kvp)
                {
                    temp.Add(lvp.Value);
                }

                return temp;
            }
        }

        public string this[string index]
        {
            get
            {
                return At(index);
            }

            set
            {
                SetOne(index, value);
            }
        }

        public void SetOne(string key, string newText)
        {
            int i = 0;
            bool canSet = false;

            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    canSet = true;
                    break;
                }
                i++;
            }

            if (canSet) SetByIndex(i, newText);
        }

        public void SetByIndex(int index, string newText)
        {
            kvp[index] = new KeyValuePair<string, string>(kvp[index].Key, newText);
        }

        public void SetByIndex(int[] indicies, string[] newText)
        {
            int loopIndex = 0;

            foreach (int i in indicies)
            {
                SetByIndex(i, newText[loopIndex]);
                loopIndex++;
            }
        }

        public void SetAll(string key, string value)
        {
            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    SetOne(key, value);
                }
            }
        }

        /// <summary>
        /// Add's an element into the Dictionary
        /// </summary>
        /// <param name="key">The key of the element (can be a duplicate)</param>
        /// <param name="value">The value of the element (can be a dublicate)</param>

        public void Add(string key, string value)
        {
            KeyValuePair<string, string> tmp = new KeyValuePair<string, string>(key, value);
            kvp.Add(tmp);
        }

        /// <summary>
        /// Remove's the first element having the same key as specified
        /// </summary>
        /// <param name="key">The key of the element to be removed</param>

        public void RemoveByKey(string key)
        {
            int index = 0;
            bool canRemove = false;

            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    canRemove = true;
                    break;
                }

                index++;
            }

            if (canRemove) kvp.RemoveAt(index);
        }

        /// <summary>
        /// Remove's all element having the same key as specified
        /// </summary>
        /// <param name="key">The key of the element(s) you want to remove</param>

        public void RemoveAllByKey(string key)
        {
            List<int> temp = new List<int>();
            int index = 0;

            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    temp.Add(index);
                }

                index++;
            }

            if (temp.Count > 0)
            {
                RemoveByIndex(temp.ToArray());
            }
        }

        /// <summary>
        /// Remove's all element from the dictionary
        /// </summary>

        public void Clear()
        {
            kvp.Clear();
        }

        /// <summary>
        /// Remove's an element with the specified index form the dictionary
        /// </summary>
        /// <param name="index">The index of the item you want ot remove</param>

        public void RemoveByIndex(int index)
        {
            kvp.RemoveAt(index);
        }

        /// <summary>
        /// Remove's multiple items specified by the indices array
        /// </summary>
        /// <param name="indicies">The int array of the element id's which you want to remove</param>

        public void RemoveByIndex(int[] indicies)
        {
            for (int i = 0; i < indicies.Length; i++)
            {
                int cIndex = indicies[i];
                kvp.RemoveAt(cIndex);

                for (int c = i; c < indicies.Length; c++)
                {
                    int lci = indicies[c];

                    if (lci > cIndex)
                    {
                        indicies[c] -= 1;
                    }
                }
            }
        }

        /// <summary>
        /// Read's the first element with the specified key
        /// </summary>
        /// <param name="key">The key of the element</param>
        /// <returns>String value</returns>

        public string At(string key)
        {
            int index = 0;

            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    return At(index);
                }

                index++;
            }

            return null;
        }

        /// <summary>
        /// Read's the value of an element based on the index specified
        /// </summary>
        /// <param name="index">Index of the element</param>
        /// <returns>String value</returns>

        public string At(int index)
        {
            if (index >= kvp.Count || kvp.Count == 0) return null;
            return kvp[index].Value;  
        }

        /// <summary>
        /// Read's multiple items with the same key
        /// </summary>
        /// <param name="key">The key of the item(s)</param>
        /// <returns>String array of values</returns>

        public IEnumerable<string> GetMultipleItems(string key)
        {
            int index = 0;

            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key)
                {
                    yield return At(index);
                }

                index++;
            }
        }

        /// <summary>
        /// Read's multiple items based on the indeicies
        /// </summary>
        /// <param name="indicies">The indicies of the requested values</param>
        /// <returns>String array of values</returns>

        public IEnumerable<string> GetMultipleItems(int[] indicies)
        {
            foreach (int i in indicies)
            {
                yield return kvp[i].Value;
            }
        }

        /// <summary>
        /// Read's wheter you have at least one element with the specified key
        /// </summary>
        /// <param name="key">The key of the element you want to search</param>
        /// <returns>True if element with the key is present</returns>

        public bool ContainsKey(string key)
        {
            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Key == key) return true;
            }

            return false;
        }

        /// <summary>
        /// Read's wheter at least one element with the same value exists
        /// </summary>
        /// <param name="value">The value of the element to search</param>
        /// <returns>True if the value is in at least on of the elements</returns>

        public bool ContainsValue(string value)
        {
            foreach (KeyValuePair<string, string> lvp in kvp)
            {
                if (lvp.Value == value) return true;
            }

            return false;
        }
    }
}