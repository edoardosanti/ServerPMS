
using Microsoft.Extensions.Logging;
using System.Data.Common;
using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Users;

namespace ServerPMS.Users
{
	public class UsersManager : IUserManager
	{
		private readonly ILogger<UsersManager> logger;
		private readonly ICommandDBAccessor cdba;
		private readonly IQueryDBAccessor qdba;
		private readonly IGlobalIDsManager globalIDsManager;

		private HashSet<User> usersList;

		public UsersManager(ILogger<UsersManager> logger, ICommandDBAccessor cdba, IQueryDBAccessor qdba, IGlobalIDsManager globalIDsManager)
		{
			this.logger = logger;
			this.cdba = cdba;
			this.qdba = qdba;
			this.globalIDsManager = globalIDsManager;

			usersList = new HashSet<User>();

        }

		public void NewOperation(UserOperationDescriptor operation)
		{
			_ =  _NewOperationDB(operation);
		}

		public async Task LoadUsersAsync()
		{
			string op = string.Format("SELECT * FROM users;");
			await qdba.QueryAsync(op, (DbDataReader dbdr) =>
			{
				User user = null;
				while (dbdr.Read())
				{
					for (int i = 0; i < dbdr.FieldCount; i++)
					{
						user = new User(dbdr.GetString(1), dbdr.GetString(2), (UserRole)dbdr.GetInt32(4), dbdr.GetInt32(0),dbdr.GetDateTime(3));
					}
					usersList.Add(user);
					globalIDsManager.AddUserEntry(user.RuntimeID, user.DBId.Value);
					
				}
				return usersList;
			});

			Console.WriteLine("---------USERS------------");
			foreach(User user in usersList)
			{
				Console.WriteLine($"Username: {user.Username} Role: {user.Role.ToString()}");
			}

		}

		private async Task _NewOperationDB(UserOperationDescriptor operation)
		{
			string sql = operation.Type switch
			{
				UserOperationType.EditOrderDetails => $"INSERT INTO user_operations(operation_id, op_type, requesting_user, order_id) VALUES(\"{operation.RuntimeID}\", {(int)operation.Type}, \"{globalIDsManager.GetUserDBID(operation.UserID)}\", \"{operation.OrderID}\");",

				_ => $"INSERT INTO users_operations(operation_id, op_type, requesting_user, unit_id) VALUES(\"{operation.RuntimeID}\", {(int)operation.Type}, \"{globalIDsManager.GetUserDBID(operation.UserID)}\", \"{operation.UnitID}\");"
			};

			_ = cdba.NewTransactionAndCommit(
				new string[] {
                sql,
                $"UPDATE users SET last_access=\"{DateTime.Now.ToShortDateString()+" "+DateTime.Now.ToShortTimeString()}\" WHERE ID = {globalIDsManager.GetUserDBID(operation.UserID)}",
                });


        }
    }

}

