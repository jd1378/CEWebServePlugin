using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESDK
{
    //Not much of an SDK but more an example of how to wrap the exposed classes by CE into C# classes. Learn from this and implement the other features you like


    class AddressList : CEObjectWrapper, IEnumerable<MemoryRecord>
    {
        public int Count { get { return GetCount(); } }
        public MemoryRecord this[int index]
        {
            get => GetMemoryRecord(index);
        }
        public IEnumerator<MemoryRecord> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return this.GetMemoryRecord(i);
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        private int GetCount()
        {
            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Count");
                lua.GetTable(-2);

                return (int)lua.ToInteger(-1);
            }
            finally
            {
                lua.SetTop(0);
            }                
        }

        public MemoryRecord GetMemoryRecord(int i)
        {
            
            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("MemoryRecord");
                lua.GetTable(-2);

                if (lua.IsTable(-1))
                {
                    lua.PushInteger(i);
                    lua.GetTable(-2); //gets index i from the Address table  (pushInteger increased the stack by 1 so the -1 turned to -2, just in case you wanted to know...)
                    
                    if (lua.IsCEObject(-1))
                        return new MemoryRecord(lua.ToCEObject(-1));
                    else
                        throw new System.IndexOutOfRangeException();

                }

                throw new System.ApplicationException("No idea what happened");
            }
            finally
            {
                lua.SetTop(0);
            }
        }
        public void GetAddressList()
        {
            try
            {
                lua.GetGlobal("getAddressList");
                if (lua.IsNil(-1))
                    throw new System.ApplicationException("You have no getAddressList (WTF)");

                int pcr = lua.PCall(0, 1);

                if (lua.IsCEObject(-1))
                    CEObject = lua.ToCEObject(-1);
                else
                    throw new System.ApplicationException("No idea what it returned");
            }
            finally
            {
                lua.SetTop(0);
            }

        }

        public AddressList()
        {
            GetAddressList();
        }


    }
}
