## Message Viewer

This application is intended to allow a user to peek onto the Azure Service Bus.

##### NOTE: This currently only supports QUEUEs. SB TOPICs are currently a work in progress.

Use:  

- Enter the connection string for the Service Bus into the Connection String textbox
  - In order to be able to remove messages from the queue, a Connection String with Manage permissions must be used.
- Enter the name of the queue to connect to
  - If you want to connect to the DeadLetter subqueue, check the checkbox
- Click "Connect To Queue"

- This will then enumerate the messages on the queue, and load them in to the listbox unformatted.
- Right click a specific message will open a context menu allowing you to View the message or Delete the message
  - Double clicking the message will open the same view as the context menu view option
- The Message Window will show some of the pertinent message details
  - Message ID (if applicable)
  - Message Expiration Date
  - Message Body
  - User Properties assigned to the message (if applicable)
    - The user properties of messages in the Dead Letter Queue will show the reason for Dead Lettering of that message
- From this window you can also delete the message by clicking the "Delete Message" Button
  
