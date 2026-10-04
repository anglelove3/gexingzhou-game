using Godot;

// Authored scenes are the only source of static nodes. Missing bindings fail visibly.
public static class SceneBindings
{
    public static T Require<T>(Node owner,string path) where T:Node =>
        owner.GetNodeOrNull<T>(path)??throw new InvalidOperationException($"场景缺少 {owner.Name}/{path}（{typeof(T).Name}）。请恢复节点或引用。");
    public static void ReportFailure(Node owner,string message)
    {
        owner.SetMeta("binding_error",message);owner.ProcessMode=Node.ProcessModeEnum.Disabled;
        for(Node? node=owner;node!=null;node=node.GetParent())
        {
            if(node is WorldView){node.SetMeta("binding_error",message);node.ProcessMode=Node.ProcessModeEnum.Disabled;}
            if(node.GetNodeOrNull<Label>("StartupError") is {} label)
            {label.Text="无法继续：\n"+message;label.Visible=true;}
        }
        GD.Print("SCENE_BINDING_REJECTED "+message);
    }
}
