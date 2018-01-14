using System.Collections;
using System.Collections.Generic;
using DigitalRune.ServiceLocation;
using UnityEngine;

public class SimpleGame
{

    private ServiceContainer _services;

    public SimpleGame()
    {
        this.Init();
    }

    public void Init()
    {

        this._services = new ServiceContainer();

        this._services.Register(typeof(IInputService),null,typeof(InputService));
        //var inputService = new InputService();
        //Debug.LogError("input hashcode->" + inputService.GetHashCode());
        //this._services.Register(typeof(IInputService), null, inputService);
        this._services.Register(typeof(IOutputerService),null,new Outputer());

    }


    public void Update()
    {
        //var inputService = this._services.GetService(typeof (IInputService)) as IInputService;
        var inputService = this._services.GetInstance<IInputService>();
        //Debug.LogError("update input hashcode->" + inputService.GetHashCode());
        var outputer = this._services.GetInstance<IOutputerService>();
        if (inputService.GetKeyDown(KeyCode.A))
        {
            outputer.Write("A is onclicked!");
        }
    }
}
