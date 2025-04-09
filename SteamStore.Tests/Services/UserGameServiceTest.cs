﻿using Moq;
using SteamStore.Repositories.Interfaces;
using SteamStore.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WebUI;
using SteamStore.Constants;
using SteamStore.Models;
using System.Collections.ObjectModel;

namespace SteamStore.Tests.Services
{
    public class UserGameServiceTest
    {
        private readonly Mock<IUserGameRepository> userRepoMock;
        private readonly Mock<IGameRepository> gameRepoMock;
        private readonly Mock<ITagRepository> tagRepoMock;
        private readonly UserGameService service;

        public UserGameServiceTest()
        {
            userRepoMock = new Mock<IUserGameRepository>();
            gameRepoMock = new Mock<IGameRepository>();
            tagRepoMock = new Mock<ITagRepository>();

            service = new UserGameService
            {
                UserGameRepository = userRepoMock.Object,
                GameRepository = gameRepoMock.Object,
                TagRepository = tagRepoMock.Object
            };
        }

        [Fact]
        public void RemoveGameFromWishlist_CallsRepository()
        {
            var game = new Game();
            service.RemoveGameFromWishlist(game);
            userRepoMock.Verify(x => x.RemoveGameFromWishlist(game), Times.Once);
        }

        [Fact]
        public void AddGameToWishlist_WhenAlreadyOwned_ThrowsException()
        {
            var game = new Game { Name = "Cyberpunk" };
            userRepoMock.Setup(x => x.IsGamePurchased(game)).Returns(true);

            var ex = Assert.Throws<Exception>(() => service.AddGameToWishlist(game));
            Assert.Equal(string.Format(ExceptionMessages.GameAlreadyOwned, "Cyberpunk"), ex.Message);
        }

        [Fact]
        public void AddGameToWishlist_WhenNotOwned_CallsRepo()
        {
            var game = new Game { Name = "Test" };
            userRepoMock.Setup(x => x.IsGamePurchased(game)).Returns(false);

            service.AddGameToWishlist(game);
            userRepoMock.Verify(x => x.AddGameToWishlist(game), Times.Once);
        }

        [Fact]
        public void AddGameToWishlist_WhenSqlException_FormatsMessage()
        {
            var game = new Game { Name = "FIFA" };
            userRepoMock.Setup(r => r.IsGamePurchased(game)).Returns(false);
            userRepoMock.Setup(r => r.AddGameToWishlist(game)).Throws(new Exception("ExecuteNonQuery failed"));

            var ex = Assert.Throws<Exception>(() => service.AddGameToWishlist(game));
            Assert.Equal(string.Format(ExceptionMessages.GameAlreadyInWishlist, "FIFA"), ex.Message);
        }

        [Fact]
        public void PurchaseGames_CalculatesPointsCorrectly()
        {
            var game = new Game { Name = "Test" };
            var games = new List<Game> { game };

            userRepoMock.SetupSequence(x => x.GetUserPointsBalance())
                        .Returns(10)
                        .Returns(15);

            service.PurchaseGames(games);

            Assert.Equal(5, service.LastEarnedPoints);
            userRepoMock.Verify(x => x.AddGameToPurchased(game), Times.Once);
            userRepoMock.Verify(x => x.RemoveGameFromWishlist(game), Times.Once);
        }

        [Fact]
        public void ComputeNoOfUserGamesForEachTag_Works()
        {
            var tag = new Tag { Tag_name = "Action" };
            var userGame = new Game { Tags = new string[]{ "Action" } };

            userRepoMock.Setup(x => x.GetAllUserGames()).Returns(new Collection<Game> { userGame });

            var tags = new Collection<Tag> { tag };
            service.ComputeNoOfUserGamesForEachTag(tags);

            Assert.Equal(1, tag.NumberOfUserGamesWithTag);
        }

        [Fact]
        public void GetFavoriteUserTags_ReturnsTop3()
        {
            var tags = new Collection<Tag>
            {
                new Tag { Tag_name = "RPG" },
                new Tag { Tag_name = "FPS" },
                new Tag { Tag_name = "MMO" },
                new Tag { Tag_name = "SPORTS" }
            };

            var games = new List<Game>
            {
                new Game { Tags = new[] { "RPG", "FPS" } },
                new Game { Tags = new[] { "RPG", "MMO" } }
            };

            tagRepoMock.Setup(x => x.GetAllTags()).Returns(tags);
            userRepoMock.Setup(x => x.GetAllUserGames()).Returns(new Collection<Game>());

            var result = service.GetFavoriteUserTags();

            Assert.Equal(3, result.Count);
            Assert.Equal("RPG", result[0].Tag_name);
        }

        [Fact]
        public void ComputeTagScoreForGames_CalculatesProperly()
        {
            var games = new Collection<Game>
            {
                new Game { Tags = new[] { "RPG", "FPS" } },
                new Game { Tags = new[] { "MMO" } }
            };

            var tags = new Collection<Tag>
            {
                new Tag { Tag_name = "RPG", NumberOfUserGamesWithTag = 3 },
                new Tag { Tag_name = "FPS", NumberOfUserGamesWithTag = 2 },
                new Tag { Tag_name = "MMO", NumberOfUserGamesWithTag = 1 }
            };


            var game = new Game { Tags = new string[]{ "RPG" } };

            tagRepoMock.Setup(x => x.GetAllTags()).Returns(tags);
            userRepoMock.Setup(x => x.GetAllUserGames()).Returns(games);

            service.ComputeTagScoreForGames(games);

            Assert.Equal(1, game.TagScore); // 3 * (1/3) = 1
        }

        [Fact]
        public void ComputeTrendingScores_SetsTrendingScore()
        {
            var games = new Collection<Game>
        {
            new Game { Name = "A", NumberOfRecentPurchases = 5 },
            new Game { Name = "B", NumberOfRecentPurchases = 10 }
        };

            service.ComputeTrendingScores(games);

            Assert.Equal(0.5m, games[0].TrendingScore);
            Assert.Equal(1.0m, games[1].TrendingScore);
        }

        [Fact]
        public void GetRecommendedGames_ReturnsTop10()
        {
            var games = new Collection<Game>
            {
                new Game { NumberOfRecentPurchases = 10, Tags = new[] { "RPG" } },
                new Game { NumberOfRecentPurchases = 20, Tags = new[] { "RPG" } }
            };

            var tags = new Collection<Tag>
            {
                new Tag { Tag_name = "RPG", NumberOfUserGamesWithTag = 2 },
            };

            gameRepoMock.Setup(x => x.GetAllGames()).Returns(games);
            tagRepoMock.Setup(x => x.GetAllTags()).Returns(tags);
            userRepoMock.Setup(x => x.GetAllUserGames()).Returns(games);

            var result = service.GetRecommendedGames();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SearchWishListByName_ReturnsMatches()
        {
            var games = new Collection<Game>
        {
            new Game { Name = "Football" },
            new Game { Name = "Horror" }
        };

            userRepoMock.Setup(x => x.GetWishlistGames()).Returns(games);

            var result = service.SearchWishListByName("foot");
            Assert.Single(result);
            Assert.Equal("Football", result[0].Name);
        }

        [Fact]
        public void FilterWishListGames_FiltersByCriteria()
        {
            var games = new Collection<Game>
        {
            new Game { Rating = 4.7m },
            new Game { Rating = 4.3m },
            new Game { Rating = 2.5m },
            new Game { Rating = 1.5m }
        };

            userRepoMock.Setup(x => x.GetWishlistGames()).Returns(games);

            Assert.Single(service.FilterWishListGames(FilterCriteria.OVERWHELMINGLYPOSITIVE));
            Assert.Single(service.FilterWishListGames(FilterCriteria.VERYPOSITIVE));
            Assert.Single(service.FilterWishListGames(FilterCriteria.MIXED));
            Assert.Single(service.FilterWishListGames(FilterCriteria.NEGATIVE));
        }

        [Fact]
        public void IsGamePurchased_DelegatesToRepository()
        {
            var game = new Game();
            userRepoMock.Setup(x => x.IsGamePurchased(game)).Returns(true);

            Assert.True(service.IsGamePurchased(game));
        }

        [Fact]
        public void SortWishListGames_SortsByCriteria()
        {
            var games = new Collection<Game>
        {
            new Game { Name = "B", Price = 20, Rating = 3, Discount = 10 },
            new Game { Name = "A", Price = 10, Rating = 5, Discount = 50 }
        };

            userRepoMock.Setup(x => x.GetWishlistGames()).Returns(games);

            var sorted = service.SortWishListGames(FilterCriteria.PRICE, true);
            Assert.Equal("A", sorted[0].Name);

            sorted = service.SortWishListGames(FilterCriteria.DISCOUNT, false);
            Assert.Equal("A", sorted[0].Name);
        }
    }
}
