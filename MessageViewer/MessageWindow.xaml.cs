using MessageViewer.Domain;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MessageViewer
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public QueueMessage SelectedMessage { get; set; }
        public IQueueClient Client { get; set; }

        public MessageWindow(QueueMessage message, IQueueClient queueClient)
        {
            InitializeComponent();

            SelectedMessage = message;
            Client = queueClient;

            lblMessageId.Text = SelectedMessage.Message.MessageId;
            lblExpiresAt.Text = SelectedMessage.Message.ExpiresAtUtc.ToString("yyyyMMdd HH:mm:ss");
            string messageText = "";
            try
            {
                dynamic desObj = JsonConvert.DeserializeObject(SelectedMessage.MessageString);
                messageText = JsonConvert.SerializeObject(desObj, new JsonSerializerSettings { Formatting = Formatting.Indented });
            }catch
            {
                messageText = SelectedMessage.MessageString;
            }
            tbxBody.Text = messageText;

            btnCloseWindow.Click += btnCloseWindow_Click;
            btnDeleteMessage.Click += btnDeleteMessage_Click;

            var userProps = message.Message.UserProperties.ToList();

            lvUserProperties.ItemsSource = userProps;
        }

        private async void btnDeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Client.CompleteAsync(SelectedMessage.LockToken);
                this.Close();
            }
            catch (MessageLockLostException mlle)
            {
                MessageBox.Show($"Lock Token expired, can't remove this message! [{mlle.Message}]");
            }
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
