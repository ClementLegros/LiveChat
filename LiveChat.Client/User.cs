namespace LiveChat.Client
{
    public class User
    {
        public string Username { get; set; }
        public string IpAddress { get; set; }

        public override string ToString()
        {
            return $"{Username} - {IpAddress}";
        }
    }
}