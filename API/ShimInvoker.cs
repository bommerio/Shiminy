namespace Shiminy.API {
    public interface ShimInvoker {
        object Invoke(string name, object[] args);
    }
}
