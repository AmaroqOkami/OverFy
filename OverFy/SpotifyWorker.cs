using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverFy
{
    public class SpotifyWorker : INotifyPropertyChanged
    {
        private Thread _workerThread;
        private CancellationTokenSource _cancellationTokenSource;
        private static SpotifyLocalAPI _spotify;
        private string _preview;

        public string Preview
        {
            get { return _preview; }
            set { _preview = value; NotifyPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        bool running = false;
        bool spotifyHooked = false;

        public SpotifyWorker()
        {
            HookSpotify();
        }

        private void HookSpotify()
        {
            _spotify = new SpotifyLocalAPI();
            if (!SpotifyLocalAPI.IsSpotifyRunning())
                return; //Make sure the spotify client is running
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
                return; //Make sure the WebHelper is running

            if (!_spotify.Connect())
                return; //We need to call Connect before fetching infos, this will handle Auth stuff

            spotifyHooked = true;
        }

        public void Start()
        {
            if (_workerThread != null || running) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _workerThread = new Thread(BackgroundWorker_DoWork)
            {
                Name = "OverFly",
                IsBackground = true
            };
            _workerThread.Start(_cancellationTokenSource.Token);
            running = true;
        }

        public void Stop()
        {
            ClearScreen();

            if (_workerThread == null || !running) return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
            _workerThread.Join();
            _workerThread = null;
            running = false;
        }

        private void BackgroundWorker_DoWork(object tokenObject)
        {
            var cancellationToken = (CancellationToken)tokenObject;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!spotifyHooked)
                    {
                        HookSpotify();
                    }

                    StringBuilder result = GetSpotifyInfo(_spotify.GetStatus());

                    Preview = result.ToString();

                    RivaTuner.print(result.ToString());

                    Task.Delay(500, cancellationToken).Wait(cancellationToken); //Wait half a second to write again
                }
                catch (OperationCanceledException)
                {
                    ClearScreen();
                    return;
                }
                catch
                {
                    ClearScreen();
                }
            }
        }

        /// <summary>
        /// Writes a empty string to the screen for cleaning rivatuner of junk because it doesn't update if the applicatin suddenly stops
        /// </summary>
        public static void ClearScreen()
        {
            RivaTuner.print(String.Empty);
        }

        private StringBuilder GetSpotifyInfo(StatusResponse currentStatus)
        {
            StringBuilder result = new StringBuilder();
            string lastItem = String.Empty;
            bool skip = false;

            if (!SpotifyLocalAPI.IsSpotifyRunning() || !SpotifyLocalAPI.IsSpotifyWebHelperRunning() || currentStatus == null)
            {
                skip = true;
            }

            for (int i = App.appSettings.PropertiesOrder.Count - 1; i > 0; i--)
            {
                if (!App.appSettings.PropertiesOrder[i].Contains("System Time"))
                {
                    lastItem = App.appSettings.PropertiesOrder[i];
                    break;
                }
            }

            if (!skip)
            {
                foreach (var item in App.appSettings.PropertiesOrder)
                {
                    switch (item)
                    {
                        case "System Time 12h":
                            result.Append(DateTime.Now.ToString("hh:mm tt", CultureInfo.InvariantCulture));
                            break;
                        case "System Time 24h":
                            result.Append(DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));
                            break;
                        case "Song Name":
                            result.Append(NormalizeString(currentStatus.Track.TrackResource.Name));
                            //We needed to normalize strings because some like ç can break rivatuner
                            break;
                        case "Artist Name":
                            result.Append(NormalizeString(currentStatus.Track.ArtistResource.Name));
                            break;
                        case "Song and Artist Name":
                            if (currentStatus.Track.TrackResource.Name != "" && currentStatus.Track.ArtistResource.Name != "")
                                result.Append(NormalizeString("\"" + currentStatus.Track.TrackResource.Name + "\" - " + currentStatus.Track.ArtistResource.Name));
                            else if (currentStatus.Track.TrackResource.Name != "" && currentStatus.Track.ArtistResource.Name == "")
                                result.Append(NormalizeString("\"" + currentStatus.Track.TrackResource.Name + "\""));
                            else
                                result.Append("Track data missing");
                            break;
                        case "Song Running Time":
                            TimeSpan runningTime = TimeSpan.FromSeconds(currentStatus.PlayingPosition);
                            TimeSpan totalTime = TimeSpan.FromSeconds(currentStatus.Track.Length);
                            result.Append(runningTime.ToString(@"mm\:ss") + "/" + totalTime.ToString(@"mm\:ss"));
                            break;
                        case "Album Name":
                            result.Append(NormalizeString(currentStatus.Track.AlbumResource.Name));
                            break;
                        case "Label":
                            result.Append("Now Playing: ");
                            break;
                        case "New Line":
                            result.Append(Environment.NewLine);
                            break;
                        default:
                            result.Append(item);
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Removes Special characters from a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string NormalizeString(string input)
        {
            ///New Method
            byte[] tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(input);
            string normalizedString = Encoding.UTF8.GetString(tempBytes);

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                if (c != '�')
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();

            ///Original Method
            //var decomposed = input.Normalize(NormalizationForm.FormD);
            //var filtered = decomposed.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            //return new String(filtered.ToArray());
        }
    }
}
