using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESDK
{
    //Not much of an SDK but more an example of how to wrap the exposed classes by CE into C# classes. Learn from this and implement the other features you like

    [JsonObject(MemberSerialization.OptIn)]
    class MemoryRecord : CEObjectWrapper
    {
        [JsonProperty]
        public int ID { get { return GetID(); } }
        [JsonProperty]
        public string Address { get { return GetAddress(); } }
        [JsonProperty]
        public string Value { get { return GetValue(); } }

        [JsonProperty]
        public RecordType Type { get { return GetRecordType(); } }

        public string CustomTypeName { get { return GetCustomRecordType(); } }
        [JsonProperty]

        public string Description { get { return GetDescription(); } }
        public Boolean Active { get { return GetActive(); } }
        public Boolean AllowIncrease { get { return GetAllowIncrease(); } }
        public Boolean AllowDecrease{ get { return GetAllowDecrease(); } }

        private int GetID()
        {
            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("ID");
                lua.GetTable(-2);

                return (int)lua.ToInteger(-1);
            }
            finally
            {
                lua.SetTop(0);
            }                
        }

        private string GetAddress()
        { 
            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Address");
                lua.GetTable(-2);
                return lua.ToString(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        private string GetDescription()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Description");
                lua.GetTable(-2);

                if (lua.IsTable(-1))
                {
                    lua.GetTable(-1);
                    return lua.ToString(-1);
                }
            }
            finally
            {
                lua.SetTop(0);
            }

            return "Error";
        }

        private string GetValue()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Value");
                lua.GetTable(-2);
                return lua.ToString(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }
        private RecordType GetRecordType()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Type");
                lua.GetTable(-2);
                return (RecordType)lua.ToInteger(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }
        
        private string GetCustomRecordType()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("CustomTypeName");
                lua.GetTable(-2);
                return lua.ToString(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        private Boolean GetActive()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("Active");
                lua.GetTable(-2);
                return lua.ToBoolean(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        private Boolean GetAllowIncrease()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("AllowIncrease");
                lua.GetTable(-2);
                return lua.ToBoolean(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        private Boolean GetAllowDecrease()
        {

            try
            {
                lua.PushCEObject(CEObject);
                lua.PushString("AllowDecrease");
                lua.GetTable(-2);
                return lua.ToBoolean(-1);
            }
            finally
            {
                lua.SetTop(0);
            }
        }

        public MemoryRecord(IntPtr ceObject)
        {
            CEObject = ceObject;
        }

        public enum RecordType
        {
            vtByte = 0,
            vtWord = 1,
            vtDword = 2,
            vtQword = 3,
            vtSingle = 4,
            vtDouble = 5,
            vtString = 6,
            vtUnicodeString = 7, // --Only used by autoguess
            vtByteArray = 8,
            vtBinary = 9,
            vtAutoAssembler = 11,
            vtPointer = 12, // --Only used by autoguess and structures
            vtCustom = 13,
            vtGrouped = 14
        }
    }
}
