using BinanceTradingBot.Domain.Entities;
using System.Linq.Expressions;

namespace BinanceTradingBot.WebDashboard.Repositories
{
    /// <summary>
    /// Interface générique de repository pour gérer les opérations CRUD de base
    /// </summary>
    /// <typeparam name="T">Type d'entité</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Récupère une entité par son identifiant
        /// </summary>
        Task<T?> GetByIdAsync(object id);
        
        /// <summary>
        /// Récupère toutes les entités
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();
        
        /// <summary>
        /// Trouve des entités qui correspondent au prédicat spécifié
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Ajoute une nouvelle entité
        /// </summary>
        Task AddAsync(T entity);
        
        /// <summary>
        /// Met à jour une entité existante
        /// </summary>
        Task UpdateAsync(T entity);
        
        /// <summary>
        /// Supprime une entité
        /// </summary>
        Task DeleteAsync(T entity);
        
        /// <summary>
        /// Sauvegarde les changements dans la base de données
        /// </summary>
        Task SaveChangesAsync();
    }

    /// <summary>
    /// Interface de repository pour les positions de trading
    /// </summary>
    public interface IPositionRepository : IRepository<Position>
    {
        /// <summary>
        /// Récupère toutes les positions actives
        /// </summary>
        Task<IEnumerable<Position>> GetActivePositionsAsync();
        
        /// <summary>
        /// Récupère les positions par symbole
        /// </summary>
        Task<IEnumerable<Position>> GetPositionsBySymbolAsync(string symbol);
        
        /// <summary>
        /// Récupère les positions dans une plage de dates
        /// </summary>
        Task<IEnumerable<Position>> GetPositionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Ferme une position
        /// </summary>
        Task<Position> ClosePositionAsync(long id, decimal exitPrice);
    }

    /// <summary>
    /// Interface de repository pour les paires de trading
    /// </summary>
    public interface ITradingPairRepository : IRepository<TradingPair>
    {
        /// <summary>
        /// Récupère une paire de trading par son symbole
        /// </summary>
        Task<TradingPair?> GetBySymbolAsync(string symbol);
        
        /// <summary>
        /// Récupère toutes les paires de trading actives
        /// </summary>
        Task<IEnumerable<TradingPair>> GetActiveAsync();
        
        /// <summary>
        /// Bascule l'état actif d'une paire de trading
        /// </summary>
        Task<bool> ToggleActiveAsync(string symbol);
    }

    /// <summary>
    /// Interface de repository pour les paramètres de l'application
    /// </summary>
    public interface ISettingsRepository
    {
        /// <summary>
        /// Récupère un paramètre par sa clé
        /// </summary>
        Task<string?> GetSettingAsync(string key);
        
        /// <summary>
        /// Met à jour un paramètre
        /// </summary>
        Task UpdateSettingAsync(string key, string value);
        
        /// <summary>
        /// Récupère tous les paramètres
        /// </summary>
        Task<IDictionary<string, string>> GetAllSettingsAsync();
        
        /// <summary>
        /// Met à jour plusieurs paramètres en une seule fois
        /// </summary>
        Task UpdateSettingsAsync(IDictionary<string, string> settings);
    }
}