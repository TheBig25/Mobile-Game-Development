#if UGS_CLOUDSAVE_INSTALLED
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;

namespace CMP5402.Week7
{
    internal sealed class UgsCloudStore : ICloudStore
    {
        public async Task<string> SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            return AuthenticationService.Instance.PlayerId;
        }

        public async Task<(string json, string writeLock)> ReadAsync(string key)
        {
            var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            return items.TryGetValue(key, out var item)
                ? (item.Value.GetAs<string>(), item.WriteLock)
                : (null, null);
        }

        public async Task<string> WriteAsync(string key, string json, string writeLock)
        {
            try
            {
                var player = CloudSaveService.Instance.Data.Player;
                var locks = writeLock == null
                    ? await player.SaveAsync(new Dictionary<string, object> { { key, json } })
                    : await player.SaveAsync(new Dictionary<string, SaveItem> { { key, new SaveItem(json, writeLock) } });
                return locks != null && locks.TryGetValue(key, out var nextLock) ? nextLock : writeLock;
            }
            catch (CloudSaveConflictException e)
            {
                throw new CloudStoreConflictException(e);
            }
        }

        public Task DeleteAsync(string key) => CloudSaveService.Instance.Data.Player.DeleteAsync(key);

        public async Task DeleteAccountAsync()
        {
            await AuthenticationService.Instance.DeleteAccountAsync();
            AuthenticationService.Instance.SignOut(true);
        }
    }
}
#endif
