namespace PrAnnotator.Core.Models
{
    public interface IPrAnnotatorModel
    {
        string EventId { get; }
        string TeamProject { get; }
        string TfsCollectionUri { get; }
    }
}