﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Users
    {
        public List<long> subscribers = new List<long>();
        //public MimeKit.MimeMessage MimeMessage = new MimeKit.MimeMessage();
        public string? Subject { get; set; }
    }
}
