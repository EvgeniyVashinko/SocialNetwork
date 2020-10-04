﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Domain.Entities
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string TagName { get; set; }
        public List<ArticleTag> ArticleTags { get; set; }
    }

}
