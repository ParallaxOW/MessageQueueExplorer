using MessageViewer.Domain;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
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
        private ObservableCollection<QueueMessage> _messages = new ObservableCollection<QueueMessage>();

        public ObservableCollection<QueueMessage> Messages
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
            btnSendMsg2.Click += new RoutedEventHandler(btnSendMsg2_Click);

            lbxMessages.MouseDoubleClick += lbxMessages_MouseDoubleClick;

            lbxMessages.ItemsSource = _messages;
            lbxMessages.DisplayMemberPath = "MessageString";
            DataContext = this;
        }

        private void lbxMessages_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageWindow msgWindow = new MessageWindow((QueueMessage)lbxMessages.SelectedItem, queueClient);
            msgWindow.Show();
        }

        private async void btnSendMsg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string messageBody = $"This is a test non-JSON message {DateTimeOffset.UtcNow}";
                Message msg = new Message(Encoding.UTF8.GetBytes(messageBody));
                msg.MessageId = Guid.NewGuid().ToString();

                msg.UserProperties.Add("DateTime", DateTime.UtcNow);
                Random rand = new Random();

                for (int i = 1; i <= 10; i++)
                {
                    msg.UserProperties.Add($"Random Number {i}", rand.Next(25000, 40000));
                }

                await queueClient.SendAsync(msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSendMsg2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string messageBody = string.Format("{{'firstname':'{0}', 'lastname':'{1}', 'date':'{2}'}}", "Russ", "Langel", DateTimeOffset.UtcNow);
                Message msg = new Message(Encoding.UTF8.GetBytes(messageBody));
                msg.MessageId = Guid.NewGuid().ToString();

                msg.UserProperties.Add("DateTime", DateTime.UtcNow);
                Random rand = new Random();

                for (int i = 1; i <= 10; i++)
                {
                    msg.UserProperties.Add($"Random Number {i}", rand.Next(10000, 25000));
                }

                await queueClient.SendAsync(msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MenuItemViewMessage_Click(object sender, RoutedEventArgs e)
        {
            if (lbxMessages.SelectedIndex == -1) return;

            QueueMessage obj = ((QueueMessage)lbxMessages.Items[lbxMessages.SelectedIndex]);

            MessageWindow msgWindow = new MessageWindow(obj, queueClient);
            msgWindow.Show();
        }

        private async void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lbxMessages.SelectedIndex == -1) return;
            QueueMessage obj = ((QueueMessage)lbxMessages.Items[lbxMessages.SelectedIndex]);

            //since we can potentially have multiple copies of the same message picked up by the message receiver
            //lets try to remove the one we're on, and if it fails because the lock expired, let's look for another one.  
            try
            {
                await queueClient.CompleteAsync(obj.LockToken);

            } catch (MessageLockLostException mlle)
            {
                MessageBox.Show("Lock expired, removing from the list.  Another copy of this message may still be in this list!");
            }
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
                    string queueName = cbxDeadLetter.IsChecked.Value ? EntityNameHelper.FormatDeadLetterPath(tbxQueue.Text) : $"{tbxQueue.Text}";
                    
                    queueClient = new QueueClient(tbxConnStrings.Text, queueName, ReceiveMode.PeekLock);
                    RegisterOnMessageHandlerAndReceiveMessages();
                    MessageBox.Show("Connection Successful!");
                }
                catch (Exception exc)
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
                QueueMessage msg = new QueueMessage { Message = message };

                App.Current.Dispatcher.Invoke((Action)delegate 
                {
                    _messages.Add(msg);
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
}
