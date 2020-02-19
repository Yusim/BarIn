using System;
using System.ServiceModel;

namespace BarIn
{


    /// <summary>
    /// Интерфейс связи смарфона с сервером
    /// </summary>
    [ServiceContract(Namespace = "http://www.aoreestr.ru/")]
    public interface IPostman
    {
        /// <summary>
        /// Отправка на сервер текста для компа
        /// </summary>
        /// <param name="CompId">Идентификатор получателя</param>
        /// <param name="Text">Текст</param>
        [OperationContract]
        void Post(Guid CompId, string Text);
    }

    /// <summary>
    /// Интерфейс связи компа с сервером
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IReciverCallback), Namespace = "http://www.aoreestr.ru/")]
    public interface IReciver
    {
        /// <summary>
        /// Регистрация на сервере
        /// </summary>
        /// <param name="CompId">Идентификатор получателя</param>
        [OperationContract(IsOneWay = true)]
        void Register(Guid CompId);

        /// <summary>
        /// Отмена регистрации на сервере
        /// </summary>
        /// <param name="CompId">Идентификатор получателя</param>
        [OperationContract(IsOneWay = true)]
        void UnRegister(Guid CompId);
    }

    /// <summary>
    /// Интерфейс связи сервера с компом
    /// </summary>
    [ServiceContract(Namespace = "http://www.aoreestr.ru/")]
    public interface IReciverCallback
    {
        /// <summary>
        /// Отправка текста
        /// </summary>
        /// <param name="Text">Текст</param>
        [OperationContract(IsOneWay = true)]
        void Send(string Text);

        /// <summary>
        /// Проверка связи
        /// </summary>
        [OperationContract]
        void Ping();
    }
}
