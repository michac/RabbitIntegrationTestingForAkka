namespace RabbitIntegrationTestingForAkka
{
    public struct RabbitEnvelope
    {
        public RabbitEnvelope(object message)
        {
            Message = message;
        }

        public object Message { get; }
    }
}