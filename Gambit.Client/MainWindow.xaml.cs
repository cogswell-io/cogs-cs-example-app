namespace Gambit.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using Data.Response;
    using GambitCore;
    using GambitCore.Util;
    using GambitData;
    using GambitSDK;
    using Model;

    public partial class MainWindow : Window
    {
        private static SocketClient client;

        private string accessKey;
        private string secretKey;
        private string clientSalt;
        private string clientSecret;
        private string currentNamespace;
        private string currentTimeStamp;
        private string eventName;

        private EventModel prevEventModel;

        public IEnumerable<NamespaceAttribute> Attributes = new List<NamespaceAttribute>();
        public ObservableCollection<ReceivedMessage> Messages = new ObservableCollection<ReceivedMessage>();

        public MainWindow()
        {
            GambitConfig.Init();
            InitializeComponent();

            accessKey = SystemSettings.Load("accessKey");
            secretKey = SystemSettings.Load("secretKey");
            clientSalt = SystemSettings.Load("clientSalt");
            clientSecret = SystemSettings.Load("clientSecret");
            currentNamespace = SystemSettings.Load("namespace");

            CheckBoxRememberKeys.IsChecked = SystemSettings.Load("save").Equals("true");
            TextBoxAccessKey.Text = accessKey;
            TextBoxSecretKey.Text = secretKey;
            TextBoxNamespace.Text = currentNamespace;
            TextBoxClientSaltEvent.Text = clientSalt;
            TextBoxClientSecretEvent.Text = clientSecret;

            messagesView.ItemsSource = Messages;

            ListViewEvent.SizeChanged += ListView_SizeChanged;
            messagesView.SizeChanged += ListView_SizeChanged;
        }



        /// <summary>
        /// Fetches the current namespace schema. 
        /// Current namspace is the one that it is entered into the textbox.
        /// </summary>
        public async void Namespace()
        {
            currentNamespace = TextBoxNamespace.Text;

            if (!ValidateNamespaceData())
            {
                return;
            };

            SystemSettings.Update("namespace", currentNamespace);

            IGambitSDKService service = new GambitSDKService();

            Response<NamespaceResponse> response = await service.NamespaceAsync(currentNamespace, accessKey, secretKey);

            if (response.IsSuccess)
            {
                Attributes = response.Result.Attributes;
                ListViewEvent.ItemsSource = Attributes;
                this.LogEventResult(response.RawData);
                return;
            }

            MessageBox.Show(response.Message);
        }

        /// <summary>
        /// Sends a new event with the values in the text boxes
        /// </summary>
        public async void Event()
        {
            clientSalt = TextBoxClientSaltEvent.Text;
            clientSecret = TextBoxClientSecretEvent.Text;
            eventName = TextBoxEventName.Text;
            currentTimeStamp = TextBoxCurrentTimestamp.Text;
            currentNamespace = TextBoxNamespace.Text;

            if (useCurrentTimestamp.IsChecked.Value)
            {
                currentTimeStamp = ServiceUtil.CurrentDate();
            }

            if (!ValidateEventData())
            {
                return;
            }

            EventModel currentEvent = PrepareEventModel();

            IGambitSDKService service = new GambitSDKService();
            var response = await service.EventAsync(currentEvent, clientSecret);

            if (response.IsSuccess)
            {
                this.LogEventResult(response.RawData);

                if (this.prevEventModel == null || !this.prevEventModel.Equals(currentEvent))
                {
                    PushAsync();
                }

                prevEventModel = currentEvent;
                return;
            }

            prevEventModel = currentEvent;
            MessageBox.Show(response.Message, "Error");
        }

        /// <summary>
        /// Prepares the event that should be sent
        /// </summary>
        /// <returns></returns>
        private EventModel PrepareEventModel()
        {
            Dictionary<string, object> myAttributes = new Dictionary<string, object>();
            foreach (var attribute in Attributes)
            {
                var typeCorrectedValue = GetTypeCorrectedValue(attribute);
                if (typeCorrectedValue != null)
                {
                    myAttributes.Add(attribute.Name, typeCorrectedValue);
                }
            }

            EventModel eventPostBody = new EventModel
            {
                ClientSalt = clientSalt,
                AccessKey = accessKey,
                EventName = eventName,
                Attributes = myAttributes,
                Namespace = currentNamespace,
                Timestamp = currentTimeStamp,
            };

            var selectedDirective = (ComboBoxItem)ComboBoxDebugDirective.SelectedItem;
            string selectedDirectiveTag = selectedDirective.Tag.ToString();
            if (selectedDirectiveTag != null && selectedDirectiveTag != "0")
            {
                eventPostBody.DebugDirective =
                    ((string)selectedDirective.Content).ToLower();
            }

            return eventPostBody;
        }

        /// <summary>
        /// Converts the namespace attribute into accurate type
        /// </summary>
        /// <param name="attribute">The namespace attribute to be converted</param>
        /// <returns>Returnes the value in the correct type</returns>
        private object GetTypeCorrectedValue(NamespaceAttribute attribute)
        {
            if (attribute == null || attribute.DataType == null)
            {
                return null;
            }

            //Boolean
            if (attribute.DataType.Equals("Boolean"))
            {
                if (!string.IsNullOrEmpty(attribute.Value))
                {
                    var atr = attribute.Value.ToUpper();
                    return atr.Equals("TRUE") || atr.Equals("T");
                }
                else
                {
                    return null;
                }
            }

            //Integer
            if (attribute.DataType.Equals("Integer"))
            {
                try
                {
                    return Int32.Parse(attribute.Value);
                }
                catch (Exception e)
                {
                    return null;
                }

            }

            //Number
            if (attribute.DataType.Equals("Number"))
            {
                try
                {
                    return Double.Parse(attribute.Value);
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            //Date
            if (string.IsNullOrWhiteSpace(attribute.Value))
            {
                return null;
            }
            return attribute.Value;
        }

        /// <summary>
        /// Shows a hint about the timestamp field
        /// </summary>
        private void ShowTimestampHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Must be formated in ISO-8601 fromat:\n YYYY-MM-DDThh:mm:ssTZD\n\n Example: \n 1016-01-07T22:23:24+00:00",
                "Help");
        }

        /// <summary>
        /// Shows a hint about the Debug directive field
        /// </summary>
        private void ShowDebugDirectiveHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Refer to: \n https://aviatainc.atlassian.net/wiki/display/GAM/Debug+Directives \n for info on what each of these does.", "Help");
        }

        /// <summary>
        /// Shows a hint about the namespace population button
        /// </summary>
        private void ShowPopulateHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Namespace, as well as secret key and access key from the setup tab are required to use this. \n\nWill populate the below table with your defined schema from the argument namespace. \n\nIf the table is already populated: \n* New attributes not already existing in the table are added as new rows.\n* Attribute rows in the table which do not match the schema are removed.\n* Attribute rows which exist in the table and match existing attributes defined in the schema will be left alone and their values preserved.", "Help");
        }

        /// <summary>
        /// Gets the formatted timestamp (current time)
        /// </summary>
        private void GetchCurrentTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = DateTime.UtcNow.ToString("s") + "+0000";  //2016-01-15T11:54+0000
        }

        /// <summary>
        /// In case the user checks the current timestamp checkbox
        /// </summary>
        private void HandleCheckTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = DateTime.UtcNow.ToString("s") + "+0000";  //2016-01-15T11:54+0000
            TextBoxCurrentTimestamp.IsEnabled = false;
        }

        /// <summary>
        /// In case the user unchecks the current timestamp checkbox
        /// </summary>
        private void HandleUncheckedTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = "";
            TextBoxCurrentTimestamp.IsEnabled = true;
        }

        /// <summary>
        /// Handles the populate namespace functionality
        /// </summary>
        private void PopulateSendNamespace(object sender, RoutedEventArgs e)
        {
            Namespace();
        }

        /// <summary>
        /// Remember the app keys
        /// </summary>
        private void EnabledChekedAppKeys(object sender, RoutedEventArgs e)
        {
            //save config
            SystemSettings.Update("save", "true");
            SystemSettings.Update("secretKey", secretKey);
            SystemSettings.Update("accessKey", accessKey);
        }

        /// <summary>
        /// Don't remember the app keys
        /// </summary>
        private void DisabledChekedAppKeys(object sender, RoutedEventArgs e)
        {
            //clear config
            SystemSettings.Update("save", "false");
            SystemSettings.Update("secretKey", "");
            SystemSettings.Update("accessKey", "");
        }

        /// <summary>
        /// Handles the sending of the events
        /// </summary>
        private void SendEvent(object sender, RoutedEventArgs e)
        {
            Event();
        }

        /// <summary>
        /// Executed when the secret key is changed
        /// </summary>
        private void ChangedSecretKey(object sender, TextChangedEventArgs e)
        {
            secretKey = TextBoxSecretKey.Text;
        }

        /// <summary>
        /// Executed when the access key is changed
        /// </summary>
        private void ChangedAccessKey(object sender, TextChangedEventArgs e)
        {
            accessKey = TextBoxAccessKey.Text;
        }

        /// <summary>
        /// Pushes a new message
        /// </summary>
        private async void PushAsync()
        {
            Dictionary<string, object> myAttributes = new Dictionary<string, object>();
            foreach (var atribute in Attributes)
            {
                var typeCorrectedValue = GetTypeCorrectedValue(atribute);
                if (typeCorrectedValue != null)
                {
                    myAttributes.Add(atribute.Name, typeCorrectedValue);
                }
            }

            PushModel pushModel = new PushModel
            {
                AccessKey = accessKey,
                ClientSalt = clientSalt,
                Namespace = TextBoxNamespace.Text,
                Attributes = myAttributes
            };

            try
            {
                IGambitSDKService service = new GambitSDKService();

                var action = new Action<MessageResponse>(message =>
                {
                    ReceivedMessage msg = new ReceivedMessage(message);

                    // Synchronize with the UI thread
                    this.messagesView.Dispatcher.BeginInvoke((Action)(() => this.Messages.Add(msg)));
                });

                if (client != null)
                {
                    await client.Stop();
                }

                client = service.PushAsync(pushModel, clientSecret, action);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }
        }

        /// <summary>
        /// Executed when the message row is selected (In the Messages tab)
        /// </summary>
        private void MessageRowSelected(object sender, SelectionChangedEventArgs e)
        {
            ReceivedMessage receivedMessage = (ReceivedMessage)messagesView.SelectedItem;
            //rawMessageView.Text = JsonConvert.SerializeObject(
            //    receivedMessage.Message.JsonData, new JsonSerializerSettings
            //    {
            //        Formatting = Formatting.Indented
            //    });

            rawMessageView.Text = receivedMessage.Message.JsonData.ToString();
            rawMessageView.TextWrapping = TextWrapping.Wrap;
        }

        /// <summary>
        /// Event handler for handling the resize of the ListView
        /// </summary>
        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnsWidth(sender as ListView);
        }

        /// <summary>
        /// Updates the columns with on resize
        /// </summary>
        /// <param name="listView">List view that should be updated</param>
        private void UpdateColumnsWidth(ListView listView)
        {
            GridView grid = listView.View as GridView;

            double gridWidth = listView.ActualWidth;
            int colCount = grid.Columns.Count;

            foreach (GridViewColumn col in grid.Columns)
            {
                col.Width = gridWidth / colCount;
            }
        }

        /// <summary>
        /// Cleans all messages in the Received Messages tab
        /// </summary>
        private void clearMessages_Click(object sender, RoutedEventArgs e)
        {
            this.Messages.Clear();
            messagesView.Items.Refresh();
        }

        private void LogEventResult(string rawData)
        {
            TextBoxEventRaw.AppendText(string.Format("[{0}] {1}{2}", ServiceUtil.CurrentDate(), rawData, Environment.NewLine));
            TextBoxEventRaw.ScrollToEnd();
        }

        /* VALIDATION */

        private bool ValidateNamespaceData()
        {
            bool isValid =
                this.IsValid("Namespace", this.currentNamespace) &&
                this.IsValid("Access Key", this.accessKey) &&
                this.IsValid("Main Secret Key", this.secretKey);

            return isValid;
        }

        private bool ValidateEventData()
        {
            bool isValid =
                this.IsValid("Namespace", this.currentNamespace) &&
                this.IsValid("Access Key", this.accessKey) &&
                this.IsValid("Client Salt", this.clientSalt) &&
                this.IsValid("Client Secret", this.clientSecret) &&
                this.IsValid("Event Name", this.eventName) &&
                this.IsValidTimeStamp(this.currentTimeStamp);

            return isValid;
        }

        private bool IsValid(string fieldName, string fieldValue)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
            {
                MessageBox.Show($"{fieldName} cannot be null or empty", "Error");
                return false;
            }

            return true;
        }

        private bool IsValidTimeStamp(string timestamp)
        {
            var datePattern = new Regex(@"^(?:[1-9]\d{3}-(?:(?:0[1-9]|1[0-2])-(?:0[1-9]|1\d|2[0-8])|(?:0[13-9]|1[0-2])-(?:29|30)|(?:0[13578]|1[02])-31)|(?:[1-9]\d(?:0[48]|[2468][048]|[13579][26])|(?:[2468][048]|[13579][26])00)-02-29)T(?:[01]\d|2[0-3]):[0-5]\d(?::[0-5]\d|\b)(?:Z|[+-][01]\d:[0-5]\d|[+-][01]\d[0-5]\d)$");

            if (!datePattern.IsMatch(timestamp))
            {
                MessageBox.Show("The Timestamp is invalid!", "Error");
                this.TextBoxCurrentTimestamp.Focus();
                return false;
            }

            return true;
        }
    }
}
