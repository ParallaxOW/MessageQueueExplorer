using Microsoft.Azure.ServiceBus;

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MessageViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        static IQueueClient queueClient;
        
        private string _connString;
        private ObservableCollection<MessageObject> _messages = new ObservableCollection<MessageObject>();

        public ObservableCollection<MessageObject> Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                NotifyPropertyChanged("Messages");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            btnConnect.Click += new RoutedEventHandler(btnConnect_Click);
            btnSendMsg.Click += new RoutedEventHandler(btnSendMsg_Click);

            lbxMessages.ItemsSource = _messages;
            lbxMessages.DisplayMemberPath = "MessageString";
            DataContext = this;
        }

        private async void btnSendMsg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string messageBody = $"This is my test message {DateTimeOffset.UtcNow}";
                Message msg = new Message(Encoding.UTF8.GetBytes(messageBody));
                msg.MessageId = Guid.NewGuid().ToString();

                await queueClient.SendAsync(msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lbxMessages.SelectedIndex == -1) return;

            MessageObject obj = ((MessageObject)lbxMessages.Items[lbxMessages.SelectedIndex]);

            var lockToken = obj.LockToken;

            await queueClient.CompleteAsync(lockToken);

            _messages.Remove(obj);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //clear the messages since we're switching queues.
            _messages.Clear();

            if (!string.IsNullOrWhiteSpace(tbxConnStrings.Text) && !string.IsNullOrWhiteSpace(tbxQueue.Text))
            {
                try
                {
                    queueClient = new QueueClient(tbxConnStrings.Text, tbxQueue.Text, ReceiveMode.PeekLock);
                    RegisterOnMessageHandlerAndReceiveMessages();
                    MessageBox.Show("Connection Successful!");
                }catch(Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Connection String and Queue Name Required!", "Error!");
            }
        }

        protected async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            try
            {
                string messageBody = $"{Encoding.UTF8.GetString(message.Body)}";
                App.Current.Dispatcher.Invoke((Action)delegate 
                {
                    MessageObject obj = new MessageObject
                    {
                        MessageString = messageBody,
                        MessageId = message.MessageId,
                        LockToken = message.SystemProperties.LockToken
                    };
                    _messages.Add(obj);
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Exception exc = exceptionReceivedEventArgs.Exception;
            MessageBox.Show(exc.Message);
            return Task.CompletedTask;
        }

        protected void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }
    }

    public class MessageObject
    {
        public string MessageString { get; set; }
        public string MessageId { get; set; }
        public string LockToken { get; set; }
    }

}
