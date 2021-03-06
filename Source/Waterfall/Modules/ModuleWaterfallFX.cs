﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Waterfall
{
  public class ModuleWaterfallFX : PartModule
  {
    // This identifies this FX module for reference elsewhere
    [KSPField(isPersistant = false)]
    public string moduleID = "";

    // This links to an EngineID from a ModuleEnginesFX
    [KSPField(isPersistant = false)]
    public string engineID = "";

    [SerializeField]
    protected SerializedData serializedData;

    [SerializeField]
    protected Dictionary<string, WaterfallController> allControllers;
    [SerializeField]
    protected List<WaterfallEffect> allFX;

    protected bool initialized = false;

    public List<WaterfallEffect> FX
    { get { return allFX; } }

    public List<WaterfallController> Controllers
    { get { return allControllers.Values.ToList(); } }



    //public virtual void OnBeforeSerialize()
    //{
    //  Utils.Log($"[ModuleWaterfallFX] Serializing");
    //  serializedData = ScriptableObject.CreateInstance<SerializedData>();

    //  ConfigNode newNode = new ConfigNode("Serialized");
    //  foreach (WaterfallEffect fx in allFX)
    //  {
    //    newNode.AddNode(fx.Save());
    //  }


    //  serializedData.SerializedString = newNode.ToString();
    //}

    //public virtual void OnAfterDeserialize()
    //{
    //  Utils.Log($"[ModuleWaterfallFX] Deserializing");

    //  OnLoad(ConfigNode.Parse(serializedData.SerializedString));

    //  Destroy(serializedData);
    //  serializedData = null;
    //}

    /// <summary>
    /// Load alll CONTROLLERS, TEMPLATES and EFFECTS
    /// </summary>
    /// <param name="node"></param>
    /// 

    public override void OnLoad(ConfigNode node)
    {
      base.OnLoad(node);
      
      ConfigNode[] controllerNodes = node.GetNodes(WaterfallConstants.ControllerNodeName);
      ConfigNode[] effectNodes = node.GetNodes(WaterfallConstants.EffectNodeName);
      ConfigNode[] templateNodes = node.GetNodes(WaterfallConstants.TemplateNodeName);

      if (initialized)
      {
        Utils.Log($"[ModuleWaterfallFX]: Cleaning up effects", LogType.Modules);
        CleanupEffects();
      }

      if (allControllers == null)
      {
        Utils.Log(String.Format("[ModuleWaterfallFX]: Loading Controllers on moduleID {0}", moduleID), LogType.Modules);
        allControllers = new Dictionary<string, WaterfallController>();
        foreach (ConfigNode controllerDataNode in controllerNodes)
        {
          string ctrlType = "throttle";
          if (!controllerDataNode.TryGetValue("linkedTo", ref ctrlType))
          {
            Utils.LogWarning(String.Format("[ModuleWaterfallFX]: Controller on moduleID {0} does not define linkedTo, setting throttle as default ", moduleID));
          }
          if (ctrlType == "throttle")
          {

            ThrottleController tCtrl = new ThrottleController(controllerDataNode);
            allControllers.Add(tCtrl.name, tCtrl);
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded Throttle Controller on moduleID {0}", moduleID), LogType.Modules);
          }
          if (ctrlType == "atmosphere_density")
          {
            AtmosphereDensityController aCtrl = new AtmosphereDensityController(controllerDataNode);
            allControllers.Add(aCtrl.name, aCtrl);
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded Atmosphere Density Controller on moduleID {0}", moduleID), LogType.Modules);
          }
          if (ctrlType == "custom")
          {
            CustomController cCtrl = new CustomController(controllerDataNode);
            allControllers.Add(cCtrl.name, cCtrl);
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded Custom Controller on moduleID {0}", moduleID), LogType.Modules);
          }
          if (ctrlType == "rcs")
          {
            RCSController rcsCtrl = new RCSController(controllerDataNode);
            allControllers.Add(rcsCtrl.name, rcsCtrl);
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded RCS Controller on moduleID {0}", moduleID), LogType.Modules);
          }
          if (ctrlType == "random")
          {
            RandomnessController rCtrl = new RandomnessController(controllerDataNode);
            
            allControllers.Add(rCtrl.name, rCtrl);
            
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded Randomness Controller on moduleID {0}", moduleID), LogType.Modules);
          }
          if (ctrlType == "light")
          {
            LightController rCtrl = new LightController(controllerDataNode);
            rCtrl.lightName = controllerDataNode.GetValue("lightName");
            allControllers.Add(rCtrl.name, rCtrl);
            Utils.Log(String.Format("[ModuleWaterfallFX]: Loaded Randomness Controller on moduleID {0}", moduleID), LogType.Modules);
          }
        }
      }

      Utils.Log(String.Format("[ModuleWaterfallFX]: Loading Effects on moduleID {0}", moduleID), LogType.Modules);

      if (allFX == null)
      {
        allFX = new List<WaterfallEffect>();
      } else
      {
        if (effectNodes.Length > 0 && allFX.Count > 0 || allFX.Count > 0 && templateNodes.Length >0)
        {
          CleanupEffects();
          allFX.Clear();
        }
      }
      
      foreach (ConfigNode fxDataNode in effectNodes)
      {
        allFX.Add(new WaterfallEffect(fxDataNode));
      }

      
      Utils.Log(String.Format("[ModuleWaterfallFX]: Loading Template effects on moduleID {0}", moduleID), LogType.Modules);
      foreach (ConfigNode templateNode in templateNodes)
      {
        string templateName = "";
        string overrideTransformName = "";
        Vector3 scaleOffset = Vector3.one;
        Vector3 positionOffset = Vector3.zero;
        Vector3 rotationOffset = Vector3.zero;


        templateNode.TryGetValue("templateName", ref templateName);
        templateNode.TryGetValue("overrideParentTransform", ref overrideTransformName);
        templateNode.TryParseVector3("scale", ref scaleOffset);
        templateNode.TryParseVector3("rotation", ref rotationOffset);
        templateNode.TryParseVector3("position", ref positionOffset);

        WaterfallEffectTemplate template = WaterfallTemplates.GetTemplate(templateName);

        foreach (WaterfallEffect fx in template.allFX)
        {
          allFX.Add(new WaterfallEffect(fx, positionOffset, rotationOffset, scaleOffset, overrideTransformName));
        }
        Utils.Log($"[ModuleWaterfallFX]: Loaded effect template {template.templateName}", LogType.Modules);
      }

      Utils.Log($"[ModuleWaterfallFX]: Finished loading {allFX.Count} effects", LogType.Modules);

      if (initialized)
      {
        Utils.Log($"[ModuleWaterfallFX]: Reinitializing", LogType.Modules);
        ReinitializeEffects();
      }

    }

    /// <summary>
    /// Dumps the entire set of EFFECTS to a confignode, then to a string
    /// </summary>
    /// <returns></returns>
    public String Export()
    {
      string toRet = "";
      foreach (WaterfallEffect fx in allFX)
      {
        toRet += $"{fx.Save().ToString()}{Environment.NewLine}";
      }
      return toRet;
    }
    public override void OnAwake()
    {
      base.OnAwake();
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (allControllers == null || allControllers.Count == 0 || allFX == null || allFX.Count == 0)
        {
          ConfigNode oldNode = FetchConfig();
          if (oldNode != null)
          Load(oldNode);
        }
      }
      }
    public void Start()
    {
     
      if (HighLogic.LoadedSceneIsFlight)
      {

        Initialize();
      }
    }

    ConfigNode FetchConfig()
    {
      ConfigNode cfg;
      foreach (UrlDir.UrlConfig pNode in GameDatabase.Instance.GetConfigs("PART"))
      {
        if (pNode.name.Replace("_", ".") == part.partInfo.name)
        {
          List<ConfigNode> fxNodes = pNode.config.GetNodes("MODULE").ToList().FindAll(n => n.GetValue("name") == moduleName);
          if (fxNodes.Count > 1)
          {
            try
            {
              return fxNodes.Single(n => n.GetValue("moduleID") == moduleID);
            }
            catch (InvalidOperationException)
            {
              // Thrown if predicate is not fulfilled, ie moduleName is not unqiue
              Utils.Log(String.Format("[ModuleWaterfallFX]: Critical configuration error: Multiple ModuleWaterfallFX nodes found with identical or no moduleName"), LogType.Modules);
            }
            catch (ArgumentNullException)
            {
              // Thrown if ModuleCryoTank is not found (a Large Problem (tm))
              Utils.Log(String.Format("[ModuleWaterfallFX]: Critical configuration error: No ModuleWaterfallFX nodes found in part"), LogType.Modules);
            }
          }
          else
          {
            return fxNodes[0];
          }
        }
      }
      return null;
    }


    // VAB Inforstrings are blank
    public string GetModuleTitle()
    {
      return "";
    }

    public override string GetInfo()
    {
      return "";
    }

    /// <summary>
    /// Gets the value of the requested controller by name
    /// </summary>
    /// <param name="controllerName"></param>
    /// <returns></returns>
    public List<float> GetControllerValue(string controllerName)
    {
      if (allControllers.ContainsKey(controllerName))
      {
        return allControllers[controllerName].Get();
      }

      return new List<float> { 0f };

    }

    /// <summary>
    /// Gets the list of controllers
    /// </summary>
    /// <param name="controllerName"></param>
    /// <returns></returns>
    public List<string> GetControllerNames()
    {
      return allControllers.Keys.ToList();

    }

    public void AddEffect(WaterfallEffect newEffect)
    {
      Utils.Log("[ModuleWaterfallFX]: Added new effect", LogType.Modules);
      allFX.Add(newEffect);
      newEffect.InitializeEffect(this, true);
    }
    public void CopyEffect(WaterfallEffect toCopy)
    {
      Utils.Log($"[ModuleWaterfallFX]: Copying effect {toCopy}", LogType.Modules);

      WaterfallEffect newEffect = new WaterfallEffect(toCopy);
      allFX.Add(newEffect);
      newEffect.InitializeEffect(this, false);
    }

    public void RemoveEffect(WaterfallEffect toRemove)
    {
      Utils.Log("[ModuleWaterfallFX]: Deleting effect", LogType.Modules);

      toRemove.CleanupEffect(this);
      allFX.Remove(toRemove);
    }
    /// <summary>
    /// Does initialization of everything woo
    /// </summary>
    protected void Initialize()
    {
      Utils.Log("[ModuleWaterfallFX]: Initializing", LogType.Modules);
      InitializeControllers();
      InitializeEffects();
      initialized = true;
    }

    /// <summary>
    /// Initialize the CONTROLLERS
    /// </summary>
    protected void InitializeControllers()
    {
      Utils.Log("[ModuleWaterfallFX]: Initializing Controllers");
      foreach (KeyValuePair<string, WaterfallController> kvp in allControllers)
      {
        Utils.Log($"[ModuleWaterfallFX]: Initializing controller {kvp.Key}");
        allControllers[kvp.Key].Initialize(this);
      }
    }

    /// <summary>
    /// Initialize the EFFECTS
    /// </summary>
    protected void InitializeEffects()
    {
      Utils.Log("[ModuleWaterfallFX]: Initializing Effects", LogType.Modules);
      for (int i = 0; i < allFX.Count; i++)
      {
        Utils.Log($"[ModuleWaterfallFX]: Initializing effect {allFX[i].name}");
        allFX[i].InitializeEffect(this, false);
      }
    }
    protected void ReinitializeEffects()
    {
      Utils.Log("[ModuleWaterfallFX]: Reitializing Effects", LogType.Modules);
      for (int i = 0; i < allFX.Count; i++)
      {
        allFX[i].InitializeEffect(this, false);
      }
    }

    protected void CleanupEffects()
    {
      Utils.Log("[ModuleWaterfallFX]: Cleanup Effects", LogType.Modules);
      for (int i = 0; i < allFX.Count; i++)
      {
        allFX[i].CleanupEffect(this);
      }
    }

    protected void LateUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        for (int i = 0; i < allFX.Count; i++)
        {
          allFX[i].Update();
        }
      }
    }

    /// <summary>
    /// Sets a specific controller's value
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void SetControllerValue(string name, float value)
    {
      if (allControllers.ContainsKey(name))
        allControllers[name].Set(value);
      else
        Utils.Log($"[ModuleWaterfallFX] Couldn't SetControllerValue for id {name}", LogType.Modules);
    }

    /// <summary>
    /// Sets this module controllers as overridden by something: its controllers will be set to override
    /// </summary>
    /// <param name="mode"></param>
    public void SetControllerOverride(bool mode)
    {
      foreach (KeyValuePair<string, WaterfallController> kvp in allControllers)
      {
        allControllers[kvp.Key].SetOverride(mode);

      }
    }

    /// <summary>
    /// Sets this module controllers as overridden by something: its controllers will be set to override
    /// </summary>
    /// <param name="mode"></param>
    public void SetControllerOverride(string name, bool mode)
    {
      if (allControllers.ContainsKey(name))
        allControllers[name].SetOverride(mode);
      else
        Utils.Log($"[ModuleWaterfallFX] Couldn't SetControllerOverride for id {name}", LogType.Modules);

    }

    /// <summary>
    /// Sets a specific controller as overridden by something: its controllers will be set to override and a value will be supplied
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void SetControllerOverrideValue(string name, float value)
    {
      if (allControllers.ContainsKey(name))
        allControllers[name].SetOverrideValue(value);
      else
        Utils.Log($"[ModuleWaterfallFX] Couldn't SetControllerOverrideValue for id {name}", LogType.Modules);
    }


  }
}
