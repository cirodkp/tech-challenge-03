﻿namespace TechChallenge01.Application.Events
{
    public class UpdateContactEvent 
    {
        public long Id {  get; set; }   
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public UpdateContactEvent()
        {
            
        }

        public UpdateContactEvent(long id, string name, string phoneNumber, string email)
        {
            Id = id;
            Name = name;
            PhoneNumber = phoneNumber;
            Email = email;
        }

    }
}
