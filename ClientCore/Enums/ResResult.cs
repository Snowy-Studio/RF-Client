namespace DTAConfig.Entity
{
    public record ResResult<T>
    {
        public string message { get; set; }
        public string code { get; set; }
        public T data { get; set; }

       
    }
}
