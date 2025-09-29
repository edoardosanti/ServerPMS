using System;
using ServerPMS.Users;
namespace ServerPMS.Abstractions.Users
{
	public interface IUserManager
	{
		void NewOperation(UserOperationDescriptor operation);
		Task LoadUsersAsync();


    }
}

