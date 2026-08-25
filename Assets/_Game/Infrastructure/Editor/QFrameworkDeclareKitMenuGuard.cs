#if UNITY_EDITOR
using UnityEditor;

namespace TPS.Infrastructure.Editor
{
    internal static class QFrameworkDeclareKitMenuGuard
    {
        private const string AddDeclareComponentMenu =
            "GameObject/QFramework/DeclareKit/@(Alt+D)Add Declare Component &d";

        [MenuItem(AddDeclareComponentMenu, true)]
        private static bool ValidateAddDeclareComponent()
        {
            return Selection.activeGameObject != null;
        }
    }
}
#endif
