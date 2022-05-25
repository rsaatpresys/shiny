namespace Shiny.Nfc;


public interface INfcManager
{
    IObservable<INfcTag[]> WhenTagsDetected();
}
