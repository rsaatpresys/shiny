using System.Globalization;

namespace Shiny.SpeechRecognition;


public interface ISpeechRecognizer
{
    /// <summary>
    /// Optimal command for listening to a sentence.  Completes when user pauses
    /// </summary>
    /// <returns></returns>
    IObservable<string> ListenUntilPause(CultureInfo? culture = null);


    /// <summary>
    /// Continuous dictation.  Returns text as made available.  Dispose to stop dictation.
    /// </summary>
    /// <returns></returns>
    IObservable<string> ContinuousDictation(CultureInfo? culture = null);


    /// <summary>
    /// When listening status changes
    /// </summary>
    /// <returns></returns>
    IObservable<bool> WhenListeningStatusChanged();
}