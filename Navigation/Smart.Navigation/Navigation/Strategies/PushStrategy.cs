namespace Smart.Navigation.Strategies
{
    using System.Threading.Tasks;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Strategy")]
    public sealed class PushStrategy : IAsyncNavigationStrategy, INavigationStrategy
    {
        private readonly object id;

        private ViewDescriptor descriptor;

        public PushStrategy(object id)
        {
            this.id = id;
        }

        public StrategyResult Initialize(INavigationController controller)
        {
            descriptor = controller.ViewMapper.FindDescriptor(id);

            return new StrategyResult(id, NavigationAttributes.Stacked);
        }

        public object ResolveToView(INavigationController controller)
        {
            return controller.CreateView(descriptor.Type);
        }

        public void UpdateStack(INavigationController controller, object toView)
        {
            // Stack new
            controller.ViewStack.Add(new ViewStackInfo(descriptor, toView));

            controller.OpenView(toView);

            // Deactive old
            var count = controller.ViewStack.Count;
            if (count > 1)
            {
                var index = count - 2;

                controller.ViewStack[index].RestoreParameter = controller.DeactivateView(controller.ViewStack[index].View);
            }
        }

        public Task UpdateStackAsync(INavigationController controller, object toView)
        {
            // Stack new
            controller.ViewStack.Add(new ViewStackInfo(descriptor, toView));

            controller.OpenView(toView);

            // Deactive old
            var count = controller.ViewStack.Count;
            if (count > 1)
            {
                var index = count - 2;

                controller.ViewStack[index].RestoreParameter = controller.DeactivateView(controller.ViewStack[index].View);
            }

            return Task.CompletedTask;
        }
    }
}
