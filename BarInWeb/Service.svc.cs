using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace BarIn
{
    public class Service : IPostman, IReciver
    {
        private static readonly Dictionary<Guid, IReciverCallback> CompList = new Dictionary<Guid, IReciverCallback>();
        private static readonly object CompListLocker = new object();

        /// <summary>
        /// Отправка текста
        /// </summary>
        /// <param name="Text">Текст</param>
        public void Post(Guid CompId, string Text)
        {
            if (CompList.ContainsKey(CompId))
                CompList[CompId].Send(Text);
        }

        /// <summary>
        /// Регистрация на сервере
        /// </summary>
        /// <param name="CompId">Идентификатор получателя</param>
        public void Register(Guid CompId)
        {
            lock (CompListLocker)
            {
                CompList[CompId] = OperationContext.Current.GetCallbackChannel<IReciverCallback>();
            }
        }

        /// <summary>
        /// Отмена регистрации на сервере
        /// </summary>
        /// <param name="CompId">Идентификатор получателя</param>
        public void UnRegister(Guid CompId)
        {
            lock (CompListLocker)
            {
                if (CompList.ContainsKey(CompId))
                    CompList.Remove(CompId);
            }
        }
    }
}
