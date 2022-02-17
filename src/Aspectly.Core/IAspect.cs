using System.Threading.Tasks;

namespace Aspectly;

public interface IAspect
{
    Task Invoke(AspectContext context, AspectDelegate next);
}

public delegate Task AspectDelegate();