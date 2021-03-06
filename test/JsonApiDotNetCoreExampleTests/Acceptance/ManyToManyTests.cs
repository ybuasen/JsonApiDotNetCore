using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bogus;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreExample;
using JsonApiDotNetCoreExample.Data;
using JsonApiDotNetCoreExample.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;

namespace JsonApiDotNetCoreExampleTests.Acceptance
{
    [Collection("WebHostCollection")]
    public sealed class ManyToManyTests
    {
        private readonly TestFixture<TestStartup> _fixture;

        private readonly Faker<Author> _authorFaker;
        private readonly Faker<Article> _articleFaker;
        private readonly Faker<Tag> _tagFaker;

        public ManyToManyTests(TestFixture<TestStartup> fixture)
        {
            _fixture = fixture;
            var context = _fixture.GetService<AppDbContext>();

            _authorFaker = new Faker<Author>()
                .RuleFor(a => a.LastName, f => f.Random.Words(2));

            _articleFaker = new Faker<Article>()
                .RuleFor(a => a.Caption, f => f.Random.AlphaNumeric(10))
                .RuleFor(a => a.Author, f => _authorFaker.Generate());

            _tagFaker = new Faker<Tag>()
                .CustomInstantiator(f => new Tag())
                .RuleFor(a => a.Name, f => f.Random.AlphaNumeric(10));
        }

        [Fact]
        public async Task Can_Fetch_Many_To_Many_Through_Id()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var article = _articleFaker.Generate();
            var tag = _tagFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = tag
            };
            context.ArticleTags.Add(articleTag);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}/tags";

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(route);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var document = JsonConvert.DeserializeObject<Document>(body);
            Assert.Single(document.ManyData);

            var tagResponse = _fixture.GetDeserializer().DeserializeMany<Tag>(body).Data.First();
            Assert.NotNull(tagResponse);
            Assert.Equal(tag.Id, tagResponse.Id);
            Assert.Equal(tag.Name, tagResponse.Name);
        }

        [Fact]
        public async Task Can_Fetch_Many_To_Many_Through_GetById_Relationship_Link()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var article = _articleFaker.Generate();
            var tag = _tagFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = tag
            };
            context.ArticleTags.Add(articleTag);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}/tags";

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(route);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var document = JsonConvert.DeserializeObject<Document>(body);
            Assert.Null(document.Included);

            var tagResponse = _fixture.GetDeserializer().DeserializeMany<Tag>(body).Data.First();
            Assert.NotNull(tagResponse);
            Assert.Equal(tag.Id, tagResponse.Id);
        }

        [Fact]
        public async Task Can_Fetch_Many_To_Many_Through_Relationship_Link()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var article = _articleFaker.Generate();
            var tag = _tagFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = tag
            };
            context.ArticleTags.Add(articleTag);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}/relationships/tags";
            
            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(route);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var document = JsonConvert.DeserializeObject<Document>(body);
            Assert.Null(document.Included);
            
            var tagResponse = _fixture.GetDeserializer().DeserializeMany<Tag>(body).Data.First();
            Assert.NotNull(tagResponse);
            Assert.Equal(tag.Id, tagResponse.Id);
        }

        [Fact]
        public async Task Can_Fetch_Many_To_Many_Without_Include()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var article = _articleFaker.Generate();
            var tag = _tagFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = tag
            };
            context.ArticleTags.Add(articleTag);
            await context.SaveChangesAsync();
            var route = $"/api/v1/articles/{article.Id}";

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(route);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var document = JsonConvert.DeserializeObject<Document>(body);
            Assert.Null(document.SingleData.Relationships["tags"].ManyData);
        }

        [Fact]
        public async Task Can_Create_Many_To_Many()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var tag = _tagFaker.Generate();
            var author = _authorFaker.Generate();
            context.Tags.Add(tag);
            context.AuthorDifferentDbContextName.Add(author);
            await context.SaveChangesAsync();

            var route = "/api/v1/articles";
            var request = new HttpRequestMessage(new HttpMethod("POST"), route);
            var content = new
            {
                data = new
                {
                    type = "articles",
                    attributes = new Dictionary<string, object>
                    {
                        {"caption", "An article with relationships"}
                    },
                    relationships = new Dictionary<string, dynamic>
                    {
                        {  "author",  new {
                            data = new
                            {
                                type = "authors",
                                id = author.StringId
                            }
                        } },
                        {  "tags", new {
                            data = new dynamic[]
                            {
                                new {
                                    type = "tags",
                                    id = tag.StringId
                                }
                            }
                        } }
                    }
                }
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(content));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(HeaderConstants.MediaType);

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.Created == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var articleResponse = _fixture.GetDeserializer().DeserializeSingle<Article>(body).Data;
            Assert.NotNull(articleResponse);

            var persistedArticle = await _fixture.Context.Articles
                .Include(a => a.ArticleTags)
                .SingleAsync(a => a.Id == articleResponse.Id);

            var persistedArticleTag = Assert.Single(persistedArticle.ArticleTags);
            Assert.Equal(tag.Id, persistedArticleTag.TagId);
        }

        [Fact]
        public async Task Can_Update_Many_To_Many()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var tag = _tagFaker.Generate();
            var article = _articleFaker.Generate();
            context.Tags.Add(tag);
            context.Articles.Add(article);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), route);
            var content = new
            {
                data = new
                {
                    type = "articles",
                    id = article.StringId,
                    relationships = new Dictionary<string, dynamic>
                    {
                        {  "tags",  new {
                            data = new [] { new
                            {
                                type = "tags",
                                id = tag.StringId
                            } }
                        } }
                    }
                }
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(content));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(HeaderConstants.MediaType);

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var articleResponse = _fixture.GetDeserializer().DeserializeSingle<Article>(body).Data;
            Assert.Null(articleResponse);

            _fixture.ReloadDbContext();
            var persistedArticle = await _fixture.Context.Articles
                .Include(a => a.ArticleTags)
                .SingleAsync(a => a.Id == article.Id);

            var persistedArticleTag = Assert.Single(persistedArticle.ArticleTags);
            Assert.Equal(tag.Id, persistedArticleTag.TagId);
        }

        [Fact]
        public async Task Can_Update_Many_To_Many_With_Complete_Replacement()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var firstTag = _tagFaker.Generate();
            var article = _articleFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = firstTag
            };
            context.ArticleTags.Add(articleTag);
            var secondTag = _tagFaker.Generate();
            context.Tags.Add(secondTag);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), route);
            var content = new
            {
                data = new
                {
                    type = "articles",
                    id = article.StringId,
                    relationships = new Dictionary<string, dynamic>
                    {
                        {  "tags",  new {
                            data = new [] { new
                            {
                                type = "tags",
                                id = secondTag.StringId
                            }  }
                        } }
                    }
                }
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(content));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(HeaderConstants.MediaType);

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var articleResponse = _fixture.GetDeserializer().DeserializeSingle<Article>(body).Data;
            Assert.Null(articleResponse);

            _fixture.ReloadDbContext();
            var persistedArticle = await _fixture.Context.Articles
                .Include("ArticleTags.Tag")
                .SingleOrDefaultAsync(a => a.Id == article.Id);
            var tag = persistedArticle.ArticleTags.Select(at => at.Tag).Single();
            Assert.Equal(secondTag.Id, tag.Id);
        }

        [Fact]
        public async Task Can_Update_Many_To_Many_With_Complete_Replacement_With_Overlap()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var firstTag = _tagFaker.Generate();
            var article = _articleFaker.Generate();
            var articleTag = new ArticleTag
            {
                Article = article,
                Tag = firstTag
            };
            context.ArticleTags.Add(articleTag);
            var secondTag = _tagFaker.Generate();
            context.Tags.Add(secondTag);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), route);
            var content = new
            {
                data = new
                {
                    type = "articles",
                    id = article.StringId,
                    relationships = new Dictionary<string, dynamic>
                    {
                        {  "tags",  new {
                            data = new [] { new
                            {
                                type = "tags",
                                id = firstTag.StringId
                            },   new
                            {
                                type = "tags",
                                id = secondTag.StringId
                            }  }
                        } }
                    }
                }
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(content));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(HeaderConstants.MediaType);

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            var articleResponse = _fixture.GetDeserializer().DeserializeSingle<Article>(body).Data;
            Assert.Null(articleResponse);

            _fixture.ReloadDbContext();
            var persistedArticle = await _fixture.Context.Articles
                .Include(a => a.ArticleTags)
                .SingleOrDefaultAsync(a => a.Id == article.Id);
            var tags = persistedArticle.ArticleTags.Select(at => at.Tag).ToList();
            Assert.Equal(2, tags.Count);
        }

        [Fact]
        public async Task Can_Update_Many_To_Many_Through_Relationship_Link()
        {
            // Arrange
            var context = _fixture.GetService<AppDbContext>();
            var tag = _tagFaker.Generate();
            var article = _articleFaker.Generate();
            context.Tags.Add(tag);
            context.Articles.Add(article);
            await context.SaveChangesAsync();

            var route = $"/api/v1/articles/{article.Id}/relationships/tags";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), route);
            var content = new
            {
                data = new[] {
                    new {
                        type = "tags",
                        id = tag.StringId
                    }
                }
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(content));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(HeaderConstants.MediaType);

            // @TODO - Use fixture
            var builder = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>();
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{route} returned {response.StatusCode} status code with payload: {body}");

            _fixture.ReloadDbContext();
            var persistedArticle = await _fixture.Context.Articles
                .Include(a => a.ArticleTags)
                .SingleAsync(a => a.Id == article.Id);

            var persistedArticleTag = Assert.Single(persistedArticle.ArticleTags);
            Assert.Equal(tag.Id, persistedArticleTag.TagId);
        }
    }
}
