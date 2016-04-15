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
    using GambitData;
    using GambitSDK;
    using Model;
    using Newtonsoft.Json;

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
        public ObservableCollection<RecievedMessage> Messages = new ObservableCollection<RecievedMessage>();

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

        }

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
                return;
            }

            MessageBox.Show(response.Message);
        }

        public async void Event()
        {
            clientSalt = TextBoxClientSaltEvent.Text;
            clientSecret = TextBoxClientSecretEvent.Text;
            eventName = TextBoxEventName.Text;
            currentTimeStamp = TextBoxCurrentTimestamp.Text;
            currentNamespace = TextBoxNamespace.Text;

            if (!ValidateEventData())
            {
                return;
            }

            EventModel currentEvent = PrepareEventModel();

            IGambitSDKService service = new GambitSDKService();
            var response = await service.EventAsync(currentEvent, clientSecret);

            if (response.IsSuccess)
            {
                TextBoxEventRaw.Text = response.RawData;

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
                ForwardAsMessage = true
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

        private void ShowTimestampHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Must be formated in ISO-8601 fromat:\n YYYY-MM-DDThh:mm:ssTZD\n\n Example: \n 1016-01-07T22:23:24+00:00",
                "Help");
        }

        private void ShowDebugDirectiveHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Refer to: \n https://aviatainc.atlassian.net/wiki/display/GAM/Debug+Directives \n for info on what each of these does.", "Help");
        }

        private void ShowPopulateHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Namespace, as well as secret key and access key from the setup tab are required to use this. \n\nWill populate the below table with your defined schema from the argument namespace. \n\nIf the table is already populated: \n* New attributes not already existing in the table are added as new rows.\n* Attribute rows in the table which do not match the schema are removed.\n* Attribute rows which exist in the table and match existing attributes defined in the schema will be left alone and their values preserved.", "Help");
        }

        private void GetchCurrentTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = DateTime.UtcNow.ToString("s") + "+0000";  //2016-01-15T11:54+0000
        }

        private void HandleCheckTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = DateTime.UtcNow.ToString("s") + "+0000";  //2016-01-15T11:54+0000
            TextBoxCurrentTimestamp.IsEnabled = false;
        }

        private void HandleUncheckedTimestamp(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentTimestamp.Text = "";
            TextBoxCurrentTimestamp.IsEnabled = true;
        }

        private void PopulateSendNamespace(object sender, RoutedEventArgs e)
        {
            Namespace();
        }

        private void EnabledChekedAppKeys(object sender, RoutedEventArgs e)
        {
            //save config
            SystemSettings.Update("save", "true");
            SystemSettings.Update("secretKey", secretKey);
            SystemSettings.Update("accessKey", accessKey);
        }

        private void DisabledChekedAppKeys(object sender, RoutedEventArgs e)
        {
            //clear config
            SystemSettings.Update("save", "false");
            SystemSettings.Update("secretKey", "");
            SystemSettings.Update("accessKey", "");
        }

        private void SendEvent(object sender, RoutedEventArgs e)
        {
            Event();
        }

        private void ChangedSecretKey(object sender, TextChangedEventArgs e)
        {
            secretKey = TextBoxSecretKey.Text;
        }

        private void ChangedAccessKey(object sender, TextChangedEventArgs e)
        {
            accessKey = TextBoxAccessKey.Text;
        }

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

                var action = new Action<MessageResponse>(delegate (MessageResponse message)
                {
                    RecievedMessage msg = new RecievedMessage()
                    {
                        Message = message,
                        DateRecieved = DateTime.UtcNow
                    };

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

        private void MessageRowSelected(object sender, SelectionChangedEventArgs e)
        {
            RecievedMessage recievedMessage = (RecievedMessage)messagesView.SelectedItem;
            rawMessageView.Text = JsonConvert.SerializeObject(recievedMessage.Message, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });
            rawMessageView.TextWrapping = TextWrapping.Wrap;
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
