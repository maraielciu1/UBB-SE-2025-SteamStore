// <copyright file="PointShopRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SteamStore.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SteamStore.Data;
    using SteamStore.Models;
    using SteamStore.Repositories.Interfaces;

    public class PointShopRepository : IPointShopRepository
    {
        private User user;
        private IDataLink data;

        public PointShopRepository(User user, IDataLink data)
        {
            this.user = user;
            this.data = data;
        }

        public List<PointShopItem> GetAllItems()
        {
            var items = new List<PointShopItem>();

            try
            {
                DataTable result = this.data.ExecuteReader("GetAllPointShopItems");

                foreach (DataRow row in result.Rows)
                {
                    var item = new PointShopItem
                    {
                        ItemIdentifier = Convert.ToInt32(row["ItemId"]),
                        Name = row["Name"].ToString(),
                        Description = row["Description"].ToString(),
                        ImagePath = row["ImagePath"].ToString(),
                        PointPrice = Convert.ToDouble(row["PointPrice"]),
                        ItemType = row["ItemType"].ToString(),
                    };

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve point shop items: {ex.Message}");
            }

            return items;
        }

        public List<PointShopItem> GetUserItems()
        {
            var items = new List<PointShopItem>();

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", this.user.UserIdentifier),
                };

                DataTable result = this.data.ExecuteReader("GetUserPointShopItems", parameters);

                foreach (DataRow row in result.Rows)
                {
                    var item = new PointShopItem
                    {
                        ItemIdentifier = Convert.ToInt32(row["ItemId"]),
                        Name = row["Name"].ToString(),
                        Description = row["Description"].ToString(),
                        ImagePath = row["ImagePath"].ToString(),
                        PointPrice = Convert.ToDouble(row["PointPrice"]),
                        ItemType = row["ItemType"].ToString(),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                    };
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve user's point shop items: {ex.Message}");
            }

            return items;
        }

        public void PurchaseItem(PointShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Cannot purchase a null item");
            }

            if (this.user == null)
            {
                throw new InvalidOperationException("User is not initialized");
            }

            if (this.user.PointsBalance < item.PointPrice)
            {
                throw new Exception("Insufficient points to purchase this item");
            }

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", this.user.UserIdentifier),
                    new SqlParameter("@ItemId", item.ItemIdentifier),
                };

                this.data.ExecuteNonQuery("PurchasePointShopItem", parameters);

                // Update user's point balance in memory
                this.user.PointsBalance -= (float)item.PointPrice;

                // Update user's point balance in the database
                this.UpdateUserPointBalance();
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to purchase item: {exception.Message}");
            }
        }

        public void ActivateItem(PointShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Cannot activate a null item");
            }

            if (this.user == null)
            {
                throw new InvalidOperationException("User is not initialized");
            }

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", this.user.UserIdentifier),
                    new SqlParameter("@ItemId", item.ItemIdentifier),
                };

                this.data.ExecuteNonQuery("ActivatePointShopItem", parameters);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to activate item: {exception.Message}");
            }
        }

        public void DeactivateItem(PointShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Cannot deactivate a null item");
            }

            if (this.user == null)
            {
                throw new InvalidOperationException("User is not initialized");
            }

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", this.user.UserIdentifier),
                    new SqlParameter("@ItemId", item.ItemIdentifier),
                };

                this.data.ExecuteNonQuery("DeactivatePointShopItem", parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deactivate item: {ex.Message}");
            }
        }

        public void UpdateUserPointBalance()
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", this.user.UserIdentifier),
                    new SqlParameter("@PointBalance", this.user.PointsBalance),
                };

                this.data.ExecuteNonQuery("UpdateUserPointBalance", parameters);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to update user point balance: {exception.Message}");
            }
        }
    }
}