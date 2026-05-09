using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.ViewModels
{
    /// <summary>
    /// DI-registered factory that produces a TaskListViewModel
    /// for a given filter without exposing IDbContextFactory to the Shell.
    /// </summary>
    public class TaskListViewModelFactory
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public TaskListViewModelFactory(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public TaskListViewModel Create(FilterType filter)
            => new TaskListViewModel(_dbFactory, filter);
    }
}
