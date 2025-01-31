using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(PlayerAspect))]
[assembly: RegisterGenericComponentType(typeof(IEnumerator<PlayerAspect>))]
