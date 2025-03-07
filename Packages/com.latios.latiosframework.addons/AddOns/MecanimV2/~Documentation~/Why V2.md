# Mecanim – Why V2?

This is just a brain dump of info regarding a Mecanim V2 implementation, analyzing what went wrong in V1, and explaining what could be done better. Mecanim V2 will largely be a community-driven effort, if it continues.

## Time

There are two ways to represent time when it comes to animation. There is realtime, and there is normalized time.

Realtime is where the animation time in clips is stored using game time values. Everything is counted in seconds. How far along into a clip, or a state in a state machine, or a transition is all stored in seconds.

Normalized time is where all these parameters are instead stored as a number between 0 and 1, which represent a progression fraction for whatever they relate to. Sometimes the integer part of a floating point number can be used to represent cycle counts.

Realtime has the obvious benefit of being directly relatable and intuitive to `Time.DeltaTime`. However, it starts to get confusing when dealing with blending of clips of different length (walking vs running) or dynamically speeding up and slowing down clips with cycle offsets. Normalized time handles these situations much cleaner.

V1 was designed to primarily use realtime as its time-in-state mechanism. It would then try to use this value when evaluating state transitions and cycles. But at the same time, it needed to account for various length clips in blend trees, so it used a baked average time. This was not correct, and the consequence would be that exit time triggers would be missed if too close to the end of the state, or there would be a skipping effect when cycling while playing a blend of clips.

GameObject Mecanim uses normalized time, and V2 should also be designed to use normalized time. Instead of adding deltaTime directly to the state at the beginning of the update, V2 should evaluate the weighted average lengths of all clips in the state using the current parameters, accounting for clip speed multipliers, and then divide deltaTime by the result to determine the advancement in the state. This gives a strongly-defined range which can be used to evaluate all transition thresholds, events, cycles, or other considerations.

## State vs Motion

Another design artifact of V1 was the `MecanimStateBlob` type. This was likely do to a miscommunication, where I had encouraged the flattening of sub-state machines, but sub-state machines may have been conflated with blend trees. Regardless, the consequence is that states, clips, and blend trees are all encapsulated within this single type that is recursed at runtime. Aside from being somewhat wasteful of memory, this decision had another unfortunate consequence. It obfuscated which values apply to state machine states, and which applied to motions (clips and blend trees).

In GameObject Mecanim, Motions are stateless. The time values that clips are sampled at and the clip weights can be completely computed via the normalized state time, parameter values, and constants stored in the Motions. But in V1, some values associated with state were only applied to clips instead (partly due to using realtime), which made reasoning about root motion particularly painful.

## What V1 Got Right

Despite its shortcomings, not every detail of V1 is off-base. The transition interrupt sequencing logic matches GameObject Mecanim. And the quality is slightly better as instead of linear blending from the interrupt time point, inertial blending is used instead. Also, blend tree weight evaluations are done correctly. This is especially valuable for 2D blend trees, as they are not particularly intuitive.

## New Things V2 Could Do

There are a few other ideas that may be worthwhile to explore in a V2 implementation. It could be nice to make a public API for evaluating an Animator Controller, so that the state machine could be updated on-demand, perhaps inside an animation graph. Additionally, `StateMachineBehaviour` could be used to decorate additional properties, such as instructing transitions to use inertial blending instead of linear blending. They could also be used as authoring for Unika scripts.

There have also been areas of development in Kinemation since development of V1 ended. These include the new root motion APIs, and masked animation sampling. A V2 may be able to leverage these improvements.

Lastly, IK, especially foot IK, is a potential candidate addition. The implementation GameObject Mecanim uses is based on a thesis paper.
