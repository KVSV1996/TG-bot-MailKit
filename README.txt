Project consists of two services:
-TG bot
- postcard

Services work independently of each other.

Mailbox adds the last letter to the MailStorage class when the trigger is triggered on a change in the number of messages in the mailbox.
On the part of the TG bot, a method is launched that constantly checks the presence of new letters in the MailStorage class, when they appear, displays the letter and deletes it.
