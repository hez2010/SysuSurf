namespace SysuH3C.Eap
{
    public class EapWorkerState
    {
        public int FailureCount { get; set; }
        public bool Succeeded { get; set; }
        public byte LastId { get; set; }
    }
}