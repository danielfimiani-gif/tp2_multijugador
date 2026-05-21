#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CreatePlayerAnimator
{
    private const string AssetPath = "Assets/_Project/Art/Animations/PlayerAnimator.controller";

    [MenuItem("Tools/TP2/Create or Rebuild Player Animator")]
    public static void CreateOrRebuild()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetPath) != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Sobrescribir PlayerAnimator",
                    $"Ya existe {AssetPath}. ¿Sobrescribir? (perdés cualquier clip asignado en este controller; los overrideControllers basados en él pueden necesitar reasignar clips a los nuevos placeholders)",
                    "Sobrescribir",
                    "Cancelar"))
                return;

            AssetDatabase.DeleteAsset(AssetPath);
        }

        var folder = System.IO.Path.GetDirectoryName(AssetPath);
        if (!AssetDatabase.IsValidFolder(folder))
            System.IO.Directory.CreateDirectory(folder);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(AssetPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idle = sm.AddState("Idle", new Vector3(280, 100));
        var run = sm.AddState("Run", new Vector3(540, 100));
        var jump = sm.AddState("Jump", new Vector3(280, 250));
        var fall = sm.AddState("Fall", new Vector3(540, 250));
        var attack1 = sm.AddState("Attack1", new Vector3(180, 400));
        var attack2 = sm.AddState("Attack2", new Vector3(380, 400));
        var attack3 = sm.AddState("Attack3", new Vector3(580, 400));
        var hit = sm.AddState("Hit", new Vector3(780, 400));
        var dead = sm.AddState("Dead", new Vector3(820, 250));

        sm.defaultState = idle;

        idle.motion = CreatePlaceholderClip("Idle", controller);
        run.motion = CreatePlaceholderClip("Run", controller);
        jump.motion = CreatePlaceholderClip("Jump", controller);
        fall.motion = CreatePlaceholderClip("Fall", controller);
        attack1.motion = CreatePlaceholderClip("Attack1", controller);
        attack2.motion = CreatePlaceholderClip("Attack2", controller);
        attack3.motion = CreatePlaceholderClip("Attack3", controller);
        hit.motion = CreatePlaceholderClip("Hit", controller);
        dead.motion = CreatePlaceholderClip("Dead", controller);

        Transition(idle, run).WithCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Transition(run, idle).WithCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnyTo(sm, jump).WithTrigger("Jump");
        Transition(jump, fall).WithCondition(AnimatorConditionMode.Less, 0f, "VerticalSpeed");
        Transition(jump, idle).WithCondition(AnimatorConditionMode.If, 0, "Grounded");
        Transition(fall, idle).WithCondition(AnimatorConditionMode.If, 0, "Grounded");

        AnyTo(sm, attack1).WithTrigger("Attack1");
        AnyTo(sm, attack2).WithTrigger("Attack2");
        AnyTo(sm, attack3).WithTrigger("Attack3");
        ExitTo(attack1, idle, 0.85f);
        ExitTo(attack2, idle, 0.85f);
        ExitTo(attack3, idle, 0.85f);

        AnyTo(sm, hit).WithTrigger("Hit");
        ExitTo(hit, idle, 0.85f);

        AnyTo(sm, dead).WithCondition(AnimatorConditionMode.If, 0, "Dead");
        Transition(dead, idle).WithCondition(AnimatorConditionMode.IfNot, 0, "Dead");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreatePlayerAnimator] {AssetPath} created/rebuilt. Reasigná clips en los overrideControllers (ahora hay Attack1, Attack2, Attack3 en vez de Attack).");
    }

    private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.1f;
        return t;
    }

    private static AnimatorStateTransition AnyTo(AnimatorStateMachine sm, AnimatorState to)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = false;
        return t;
    }

    private static AnimatorStateTransition ExitTo(AnimatorState from, AnimatorState to, float exitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.duration = 0.1f;
        return t;
    }

    private static AnimationClip CreatePlaceholderClip(string name, AnimatorController owner)
    {
        var clip = new AnimationClip { name = name, frameRate = 10f };
        AssetDatabase.AddObjectToAsset(clip, owner);
        return clip;
    }
}

internal static class AnimatorTransitionExtensions
{
    public static AnimatorStateTransition WithCondition(this AnimatorStateTransition t, AnimatorConditionMode mode, float threshold, string param)
    {
        t.AddCondition(mode, threshold, param);
        return t;
    }

    public static AnimatorStateTransition WithTrigger(this AnimatorStateTransition t, string trigger)
    {
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        return t;
    }
}
#endif
