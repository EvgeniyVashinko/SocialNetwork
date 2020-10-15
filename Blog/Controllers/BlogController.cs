﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blog.Domain;
using Blog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers
{
    [Authorize]
    public class BlogController : Controller
    {
        private readonly DataManager dataManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<User> userManager;
        public BlogController(DataManager dataManager, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            this.dataManager = dataManager;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
        }
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewBag.Head = "Новый пост";
            var article = id == default ? new Article() : dataManager.Articles.GetArticle(id);
            if (article.Id != default)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (user.Id != article.UserId)
                {
                    return View("NoAccessPage");
                }
            }
            return View(article);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Article model, IFormFile imgFile, string category)
        {
            if (ModelState.IsValid)
            {
                Category cat = dataManager.Categories.GetCategory(category);
                model.User = await userManager.GetUserAsync(HttpContext.User);
                model.Category = cat;
                if (imgFile != null)
                {
                    model.ImagePath = imgFile.FileName;
                    using(var stream = new FileStream(Path.Combine(webHostEnvironment.WebRootPath, "images/", imgFile.FileName), FileMode.Create))
                    {
                        imgFile.CopyTo(stream);
                    }
                }
                dataManager.Articles.SaveArticle(model);
                return RedirectToAction(nameof(BlogController.EditArticles), nameof(BlogController).Replace("Controller", ""));
            }
            return View(model);
        }

        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var article = dataManager.Articles.GetArticle(id);
            if (user.Id != article.UserId)
            {
                return View("NoAccessPage");
            }
            return View(article);
        }
        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            dataManager.Articles.DeleteArticle(id);
            return RedirectToAction(nameof(BlogController.EditArticles), nameof(BlogController).Replace("Controller", ""));
        }

        public async Task<IActionResult> UserArticles()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            return View(dataManager.Articles.GetArticlesByUser(user));
        }

        public async Task<IActionResult> EditArticles()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            return View(dataManager.Articles.GetArticlesByUser(user));
        }
        [AllowAnonymous]
        public IActionResult FreshArticles()
        {
            var articles = from article in dataManager.Articles.GetArticles()
                           where article.PublishDate >= DateTime.Now.AddDays(-7)
                           orderby article.PublishDate descending
                           select article;
            ViewBag.cntrl = "Blog";
            ViewBag.actn = "FreshArticles";
            return View(articles);
        }

        //Добавить лайки к записям и выбирать по наиболее популярным
        [AllowAnonymous]
        public IActionResult PopularArticles()
        {
            var articles = from article in dataManager.Articles.GetArticles()
                           orderby article.ArticleLikes.Count() descending
                           select article;
            ViewBag.cntrl = "Blog";
            ViewBag.actn = "PopularArticles";
            return View("FreshArticles", articles);
        }
        [AllowAnonymous]
        public IActionResult Show(Guid id)
        {
            var article = dataManager.Articles.GetArticle(id);

            var comments = dataManager.Comments.GetCommentsByArticle(dataManager.Articles.GetArticle(id));
            ViewBag.Comments = comments;
            ViewBag.LikeAmount = dataManager.Articles.LikeAmount(article);
            return View(article);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(string tarea, Guid id)
        {
            Comment comment = new Comment();
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return View("Show", dataManager.Articles.GetArticle(id)); ;
            }
            comment.Article = dataManager.Articles.GetArticle(id);
            comment.User = user;
            comment.Text = tarea;
            dataManager.Comments.SaveComment(comment);

            var comments = dataManager.Comments.GetCommentsByArticle(comment.Article);

            ViewBag.Comments = comments;
            ViewBag.LikeAmount = dataManager.Articles.LikeAmount(comment.Article);
            return View("Show", dataManager.Articles.GetArticle(id));
        }
        public async Task<IActionResult> ArticleLike(Guid id, string cntrl = null, string actn = null, string name = null)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var article = dataManager.Articles.GetArticle(id);
            if (!dataManager.Articles.IsLike(user,article))
            {
                dataManager.Articles.AddLike(user, article);
            }
            else
            {
                dataManager.Articles.DeleteLike(user, article);
            }

            var comments = dataManager.Comments.GetCommentsByArticle(article);

            ViewBag.Comments = comments;
            ViewBag.LikeAmount = dataManager.Articles.LikeAmount(article);
            if (actn == null)
            {
                return View("Show", article);
            }
            return RedirectToAction(actn,cntrl,new { name = name});
        }
        [AllowAnonymous]
        public IActionResult Category(string name)
        {
            var cat = dataManager.Categories.GetCategory(name);
            var articles = dataManager.Categories.GetArticlesByCategory(cat);
            ViewBag.cntrl = "Blog";
            ViewBag.actn = "Category";
            ViewBag.name = name;
            return View("FreshArticles", articles);
        }
    }
}
