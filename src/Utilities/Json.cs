namespace RoboticInbox.Utilities
{
    internal class Json<T>
    {
        public static string Serialize(T data)
        {
            return SimpleJson2.SimpleJson2.SerializeObject(data);
        }

        public static T Deserialize(string json)
        {
            return (T)SimpleJson2.SimpleJson2.DeserializeObject(json, typeof(T));
        }
    }
}
