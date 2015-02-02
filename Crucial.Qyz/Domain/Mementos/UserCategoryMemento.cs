﻿using Crucial.Framework.DesignPatterns.CQRS.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucial.Qyz.Domain.Mementos
{
    internal class UserCategoryMemento : BaseMemento
    {
        public string Name { get; set; }

        public int EventVersion { get; set; }

        public UserCategoryMemento(int id, string name, int version)
        {
            Id = id;
            Name = name;
            Version = version;
            EventVersion = version;
        }
    }
}
