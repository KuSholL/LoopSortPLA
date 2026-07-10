var Deserializers = {}
Deserializers["UnityEngine.JointSpring"] = function (request, data, root) {
  var i762 = root || request.c( 'UnityEngine.JointSpring' )
  var i763 = data
  i762.spring = i763[0]
  i762.damper = i763[1]
  i762.targetPosition = i763[2]
  return i762
}

Deserializers["UnityEngine.JointMotor"] = function (request, data, root) {
  var i764 = root || request.c( 'UnityEngine.JointMotor' )
  var i765 = data
  i764.m_TargetVelocity = i765[0]
  i764.m_Force = i765[1]
  i764.m_FreeSpin = i765[2]
  return i764
}

Deserializers["UnityEngine.JointLimits"] = function (request, data, root) {
  var i766 = root || request.c( 'UnityEngine.JointLimits' )
  var i767 = data
  i766.m_Min = i767[0]
  i766.m_Max = i767[1]
  i766.m_Bounciness = i767[2]
  i766.m_BounceMinVelocity = i767[3]
  i766.m_ContactDistance = i767[4]
  i766.minBounce = i767[5]
  i766.maxBounce = i767[6]
  return i766
}

Deserializers["UnityEngine.JointDrive"] = function (request, data, root) {
  var i768 = root || request.c( 'UnityEngine.JointDrive' )
  var i769 = data
  i768.m_PositionSpring = i769[0]
  i768.m_PositionDamper = i769[1]
  i768.m_MaximumForce = i769[2]
  i768.m_UseAcceleration = i769[3]
  return i768
}

Deserializers["UnityEngine.SoftJointLimitSpring"] = function (request, data, root) {
  var i770 = root || request.c( 'UnityEngine.SoftJointLimitSpring' )
  var i771 = data
  i770.m_Spring = i771[0]
  i770.m_Damper = i771[1]
  return i770
}

Deserializers["UnityEngine.SoftJointLimit"] = function (request, data, root) {
  var i772 = root || request.c( 'UnityEngine.SoftJointLimit' )
  var i773 = data
  i772.m_Limit = i773[0]
  i772.m_Bounciness = i773[1]
  i772.m_ContactDistance = i773[2]
  return i772
}

Deserializers["UnityEngine.WheelFrictionCurve"] = function (request, data, root) {
  var i774 = root || request.c( 'UnityEngine.WheelFrictionCurve' )
  var i775 = data
  i774.m_ExtremumSlip = i775[0]
  i774.m_ExtremumValue = i775[1]
  i774.m_AsymptoteSlip = i775[2]
  i774.m_AsymptoteValue = i775[3]
  i774.m_Stiffness = i775[4]
  return i774
}

Deserializers["UnityEngine.JointAngleLimits2D"] = function (request, data, root) {
  var i776 = root || request.c( 'UnityEngine.JointAngleLimits2D' )
  var i777 = data
  i776.m_LowerAngle = i777[0]
  i776.m_UpperAngle = i777[1]
  return i776
}

Deserializers["UnityEngine.JointMotor2D"] = function (request, data, root) {
  var i778 = root || request.c( 'UnityEngine.JointMotor2D' )
  var i779 = data
  i778.m_MotorSpeed = i779[0]
  i778.m_MaximumMotorTorque = i779[1]
  return i778
}

Deserializers["UnityEngine.JointSuspension2D"] = function (request, data, root) {
  var i780 = root || request.c( 'UnityEngine.JointSuspension2D' )
  var i781 = data
  i780.m_DampingRatio = i781[0]
  i780.m_Frequency = i781[1]
  i780.m_Angle = i781[2]
  return i780
}

Deserializers["UnityEngine.JointTranslationLimits2D"] = function (request, data, root) {
  var i782 = root || request.c( 'UnityEngine.JointTranslationLimits2D' )
  var i783 = data
  i782.m_LowerTranslation = i783[0]
  i782.m_UpperTranslation = i783[1]
  return i782
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material"] = function (request, data, root) {
  var i784 = root || new pc.UnityMaterial()
  var i785 = data
  i784.name = i785[0]
  request.r(i785[1], i785[2], 0, i784, 'shader')
  i784.renderQueue = i785[3]
  i784.enableInstancing = !!i785[4]
  var i787 = i785[5]
  var i786 = []
  for(var i = 0; i < i787.length; i += 1) {
    i786.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter', i787[i + 0]) );
  }
  i784.floatParameters = i786
  var i789 = i785[6]
  var i788 = []
  for(var i = 0; i < i789.length; i += 1) {
    i788.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter', i789[i + 0]) );
  }
  i784.colorParameters = i788
  var i791 = i785[7]
  var i790 = []
  for(var i = 0; i < i791.length; i += 1) {
    i790.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter', i791[i + 0]) );
  }
  i784.vectorParameters = i790
  var i793 = i785[8]
  var i792 = []
  for(var i = 0; i < i793.length; i += 1) {
    i792.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter', i793[i + 0]) );
  }
  i784.textureParameters = i792
  var i795 = i785[9]
  var i794 = []
  for(var i = 0; i < i795.length; i += 1) {
    i794.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag', i795[i + 0]) );
  }
  i784.materialFlags = i794
  return i784
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter"] = function (request, data, root) {
  var i798 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter' )
  var i799 = data
  i798.name = i799[0]
  i798.value = i799[1]
  return i798
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter"] = function (request, data, root) {
  var i802 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter' )
  var i803 = data
  i802.name = i803[0]
  i802.value = new pc.Color(i803[1], i803[2], i803[3], i803[4])
  return i802
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter"] = function (request, data, root) {
  var i806 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter' )
  var i807 = data
  i806.name = i807[0]
  i806.value = new pc.Vec4( i807[1], i807[2], i807[3], i807[4] )
  return i806
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter"] = function (request, data, root) {
  var i810 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter' )
  var i811 = data
  i810.name = i811[0]
  request.r(i811[1], i811[2], 0, i810, 'value')
  return i810
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag"] = function (request, data, root) {
  var i814 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag' )
  var i815 = data
  i814.name = i815[0]
  i814.enabled = !!i815[1]
  return i814
}

Deserializers["Luna.Unity.DTO.UnityEngine.Textures.Texture2D"] = function (request, data, root) {
  var i816 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Textures.Texture2D' )
  var i817 = data
  i816.name = i817[0]
  i816.width = i817[1]
  i816.height = i817[2]
  i816.mipmapCount = i817[3]
  i816.anisoLevel = i817[4]
  i816.filterMode = i817[5]
  i816.hdr = !!i817[6]
  i816.format = i817[7]
  i816.wrapMode = i817[8]
  i816.alphaIsTransparency = !!i817[9]
  i816.alphaSource = i817[10]
  i816.graphicsFormat = i817[11]
  i816.sRGBTexture = !!i817[12]
  i816.desiredColorSpace = i817[13]
  i816.wrapU = i817[14]
  i816.wrapV = i817[15]
  return i816
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh"] = function (request, data, root) {
  var i818 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh' )
  var i819 = data
  i818.name = i819[0]
  i818.halfPrecision = !!i819[1]
  i818.useSimplification = !!i819[2]
  i818.useUInt32IndexFormat = !!i819[3]
  i818.vertexCount = i819[4]
  i818.aabb = i819[5]
  var i821 = i819[6]
  var i820 = []
  for(var i = 0; i < i821.length; i += 1) {
    i820.push( !!i821[i + 0] );
  }
  i818.streams = i820
  i818.vertices = i819[7]
  var i823 = i819[8]
  var i822 = []
  for(var i = 0; i < i823.length; i += 1) {
    i822.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh', i823[i + 0]) );
  }
  i818.subMeshes = i822
  var i825 = i819[9]
  var i824 = []
  for(var i = 0; i < i825.length; i += 16) {
    i824.push( new pc.Mat4().setData(i825[i + 0], i825[i + 1], i825[i + 2], i825[i + 3],  i825[i + 4], i825[i + 5], i825[i + 6], i825[i + 7],  i825[i + 8], i825[i + 9], i825[i + 10], i825[i + 11],  i825[i + 12], i825[i + 13], i825[i + 14], i825[i + 15]) );
  }
  i818.bindposes = i824
  var i827 = i819[10]
  var i826 = []
  for(var i = 0; i < i827.length; i += 1) {
    i826.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape', i827[i + 0]) );
  }
  i818.blendShapes = i826
  return i818
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh"] = function (request, data, root) {
  var i832 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh' )
  var i833 = data
  i832.triangles = i833[0]
  return i832
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape"] = function (request, data, root) {
  var i838 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape' )
  var i839 = data
  i838.name = i839[0]
  var i841 = i839[1]
  var i840 = []
  for(var i = 0; i < i841.length; i += 1) {
    i840.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame', i841[i + 0]) );
  }
  i838.frames = i840
  return i838
}

Deserializers["Luna.Unity.DTO.UnityEngine.Textures.Cubemap"] = function (request, data, root) {
  var i842 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Textures.Cubemap' )
  var i843 = data
  i842.name = i843[0]
  i842.atlasId = i843[1]
  i842.mipmapCount = i843[2]
  i842.hdr = !!i843[3]
  i842.size = i843[4]
  i842.anisoLevel = i843[5]
  i842.filterMode = i843[6]
  var i845 = i843[7]
  var i844 = []
  for(var i = 0; i < i845.length; i += 4) {
    i844.push( UnityEngine.Rect.MinMaxRect(i845[i + 0], i845[i + 1], i845[i + 2], i845[i + 3]) );
  }
  i842.rects = i844
  i842.wrapU = i843[8]
  i842.wrapV = i843[9]
  return i842
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Transform"] = function (request, data, root) {
  var i848 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Transform' )
  var i849 = data
  i848.position = new pc.Vec3( i849[0], i849[1], i849[2] )
  i848.scale = new pc.Vec3( i849[3], i849[4], i849[5] )
  i848.rotation = new pc.Quat(i849[6], i849[7], i849[8], i849[9])
  return i848
}

Deserializers["HiddenCarrierVisual"] = function (request, data, root) {
  var i850 = root || request.c( 'HiddenCarrierVisual' )
  var i851 = data
  var i853 = i851[0]
  var i852 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Renderer')))
  for(var i = 0; i < i853.length; i += 2) {
  request.r(i853[i + 0], i853[i + 1], 1, i852, '')
  }
  i850.targetRenderers = i852
  i850.flyDistance = i851[1]
  i850.flyDuration = i851[2]
  i850.rotateDuration = i851[3]
  i850.flyOutDuration = i851[4]
  i850.screenMargin = i851[5]
  return i850
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.MeshFilter"] = function (request, data, root) {
  var i856 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.MeshFilter' )
  var i857 = data
  request.r(i857[0], i857[1], 0, i856, 'sharedMesh')
  return i856
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.MeshRenderer"] = function (request, data, root) {
  var i858 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.MeshRenderer' )
  var i859 = data
  request.r(i859[0], i859[1], 0, i858, 'additionalVertexStreams')
  i858.enabled = !!i859[2]
  request.r(i859[3], i859[4], 0, i858, 'sharedMaterial')
  var i861 = i859[5]
  var i860 = []
  for(var i = 0; i < i861.length; i += 2) {
  request.r(i861[i + 0], i861[i + 1], 2, i860, '')
  }
  i858.sharedMaterials = i860
  i858.receiveShadows = !!i859[6]
  i858.shadowCastingMode = i859[7]
  i858.sortingLayerID = i859[8]
  i858.sortingOrder = i859[9]
  i858.lightmapIndex = i859[10]
  i858.lightmapSceneIndex = i859[11]
  i858.lightmapScaleOffset = new pc.Vec4( i859[12], i859[13], i859[14], i859[15] )
  i858.lightProbeUsage = i859[16]
  i858.reflectionProbeUsage = i859[17]
  return i858
}

Deserializers["Luna.Unity.DTO.UnityEngine.Scene.GameObject"] = function (request, data, root) {
  var i864 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Scene.GameObject' )
  var i865 = data
  i864.name = i865[0]
  i864.tagId = i865[1]
  i864.enabled = !!i865[2]
  i864.isStatic = !!i865[3]
  i864.layer = i865[4]
  return i864
}

Deserializers["SpecialColorReceiverVisual"] = function (request, data, root) {
  var i866 = root || request.c( 'SpecialColorReceiverVisual' )
  var i867 = data
  var i869 = i867[0]
  var i868 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Renderer')))
  for(var i = 0; i < i869.length; i += 2) {
  request.r(i869[i + 0], i869[i + 1], 1, i868, '')
  }
  i866.targetRenderers = i868
  return i866
}

Deserializers["LinkedBlockVisual"] = function (request, data, root) {
  var i870 = root || request.c( 'LinkedBlockVisual' )
  var i871 = data
  request.r(i871[0], i871[1], 0, i870, 'meshRenderer')
  request.r(i871[2], i871[3], 0, i870, 'skinnedMeshRenderer')
  request.r(i871[4], i871[5], 0, i870, 'modelGO')
  i870.shapeType = i871[6]
  request.r(i871[7], i871[8], 0, i870, 'catFace')
  request.r(i871[9], i871[10], 0, i870, 'progressAnimator')
  var i873 = i871[11]
  var i872 = []
  for(var i = 0; i < i873.length; i += 2) {
  request.r(i873[i + 0], i873[i + 1], 2, i872, '')
  }
  i870.keyRenderers = i872
  request.r(i871[12], i871[13], 0, i870, 'leftLinkAnchor')
  request.r(i871[14], i871[15], 0, i870, 'rightLinkAnchor')
  return i870
}

Deserializers["BlockSolidProgressAnimator"] = function (request, data, root) {
  var i876 = root || request.c( 'BlockSolidProgressAnimator' )
  var i877 = data
  request.r(i877[0], i877[1], 0, i876, 'animatedTarget')
  i876.isBlock4X = !!i877[2]
  request.r(i877[3], i877[4], 0, i876, '_animator')
  return i876
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.BoxCollider"] = function (request, data, root) {
  var i878 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.BoxCollider' )
  var i879 = data
  i878.center = new pc.Vec3( i879[0], i879[1], i879[2] )
  i878.size = new pc.Vec3( i879[3], i879[4], i879[5] )
  i878.enabled = !!i879[6]
  i878.isTrigger = !!i879[7]
  request.r(i879[8], i879[9], 0, i878, 'material')
  return i878
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Animator"] = function (request, data, root) {
  var i880 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Animator' )
  var i881 = data
  request.r(i881[0], i881[1], 0, i880, 'animatorController')
  request.r(i881[2], i881[3], 0, i880, 'avatar')
  i880.updateMode = i881[4]
  i880.hasTransformHierarchy = !!i881[5]
  i880.applyRootMotion = !!i881[6]
  var i883 = i881[7]
  var i882 = []
  for(var i = 0; i < i883.length; i += 2) {
  request.r(i883[i + 0], i883[i + 1], 2, i882, '')
  }
  i880.humanBones = i882
  i880.enabled = !!i881[8]
  return i880
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.SpriteRenderer"] = function (request, data, root) {
  var i886 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.SpriteRenderer' )
  var i887 = data
  i886.color = new pc.Color(i887[0], i887[1], i887[2], i887[3])
  request.r(i887[4], i887[5], 0, i886, 'sprite')
  i886.flipX = !!i887[6]
  i886.flipY = !!i887[7]
  i886.drawMode = i887[8]
  i886.size = new pc.Vec2( i887[9], i887[10] )
  i886.tileMode = i887[11]
  i886.adaptiveModeThreshold = i887[12]
  i886.maskInteraction = i887[13]
  i886.spriteSortPoint = i887[14]
  i886.enabled = !!i887[15]
  request.r(i887[16], i887[17], 0, i886, 'sharedMaterial')
  var i889 = i887[18]
  var i888 = []
  for(var i = 0; i < i889.length; i += 2) {
  request.r(i889[i + 0], i889[i + 1], 2, i888, '')
  }
  i886.sharedMaterials = i888
  i886.receiveShadows = !!i887[19]
  i886.shadowCastingMode = i887[20]
  i886.sortingLayerID = i887[21]
  i886.sortingOrder = i887[22]
  i886.lightmapIndex = i887[23]
  i886.lightmapSceneIndex = i887[24]
  i886.lightmapScaleOffset = new pc.Vec4( i887[25], i887[26], i887[27], i887[28] )
  i886.lightProbeUsage = i887[29]
  i886.reflectionProbeUsage = i887[30]
  return i886
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer"] = function (request, data, root) {
  var i890 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer' )
  var i891 = data
  request.r(i891[0], i891[1], 0, i890, 'sharedMesh')
  var i893 = i891[2]
  var i892 = []
  for(var i = 0; i < i893.length; i += 2) {
  request.r(i893[i + 0], i893[i + 1], 2, i892, '')
  }
  i890.bones = i892
  i890.updateWhenOffscreen = !!i891[3]
  i890.localBounds = i891[4]
  request.r(i891[5], i891[6], 0, i890, 'rootBone')
  var i895 = i891[7]
  var i894 = []
  for(var i = 0; i < i895.length; i += 1) {
    i894.push( request.d('Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer+BlendShapeWeight', i895[i + 0]) );
  }
  i890.blendShapesWeights = i894
  i890.enabled = !!i891[8]
  request.r(i891[9], i891[10], 0, i890, 'sharedMaterial')
  var i897 = i891[11]
  var i896 = []
  for(var i = 0; i < i897.length; i += 2) {
  request.r(i897[i + 0], i897[i + 1], 2, i896, '')
  }
  i890.sharedMaterials = i896
  i890.receiveShadows = !!i891[12]
  i890.shadowCastingMode = i891[13]
  i890.sortingLayerID = i891[14]
  i890.sortingOrder = i891[15]
  i890.lightmapIndex = i891[16]
  i890.lightmapSceneIndex = i891[17]
  i890.lightmapScaleOffset = new pc.Vec4( i891[18], i891[19], i891[20], i891[21] )
  i890.lightProbeUsage = i891[22]
  i890.reflectionProbeUsage = i891[23]
  return i890
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer+BlendShapeWeight"] = function (request, data, root) {
  var i900 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer+BlendShapeWeight' )
  var i901 = data
  i900.weight = i901[0]
  return i900
}

Deserializers["Block"] = function (request, data, root) {
  var i902 = root || request.c( 'Block' )
  var i903 = data
  request.r(i903[0], i903[1], 0, i902, 'blockCollider')
  request.r(i903[2], i903[3], 0, i902, 'blockVisual')
  request.r(i903[4], i903[5], 0, i902, 'containerKey')
  request.r(i903[6], i903[7], 0, i902, 'animationPivot')
  request.r(i903[8], i903[9], 0, i902, 'leftLinkAnchor')
  request.r(i903[10], i903[11], 0, i902, 'rightLinkAnchor')
  i902.blockColorType = i903[12]
  i902.blockColor = new pc.Color(i903[13], i903[14], i903[15], i903[16])
  i902.blockShadowColor = new pc.Color(i903[17], i903[18], i903[19], i903[20])
  i902.hasContent = !!i903[21]
  i902.maxCubes = i903[22]
  i902.currentCubes = i903[23]
  i902.visualCubes = i903[24]
  i902.state = i903[25]
  return i902
}

Deserializers["BlockVisual"] = function (request, data, root) {
  var i904 = root || request.c( 'BlockVisual' )
  var i905 = data
  request.r(i905[0], i905[1], 0, i904, 'fixedVisual')
  request.r(i905[2], i905[3], 0, i904, 'normalMaterial')
  request.r(i905[4], i905[5], 0, i904, 'mechanicMaterial')
  request.r(i905[6], i905[7], 0, i904, 'hiddenRevealVfxPrefab')
  request.r(i905[8], i905[9], 0, i904, 'mergeVfx')
  request.r(i905[10], i905[11], 0, i904, 'parrentVFX')
  return i904
}

Deserializers["ContainerKey"] = function (request, data, root) {
  var i906 = root || request.c( 'ContainerKey' )
  var i907 = data
  request.r(i907[0], i907[1], 0, i906, 'keyPrefab')
  i906.unlockDuration = i907[2]
  i906.unlockEase = i907[3]
  i906.useOverrideFlightY = !!i907[4]
  i906.flightY = i907[5]
  return i906
}

Deserializers["BlockSolidVisual"] = function (request, data, root) {
  var i908 = root || request.c( 'BlockSolidVisual' )
  var i909 = data
  request.r(i909[0], i909[1], 0, i908, 'parentBlock')
  request.r(i909[2], i909[3], 0, i908, 'meshRenderer')
  request.r(i909[4], i909[5], 0, i908, 'questionMarkRenderer')
  var i911 = i909[6]
  var i910 = []
  for(var i = 0; i < i911.length; i += 2) {
  request.r(i911[i + 0], i911[i + 1], 2, i910, '')
  }
  i908.keyRenderers = i910
  request.r(i909[7], i909[8], 0, i908, 'swapArrowRenderer')
  request.r(i909[9], i909[10], 0, i908, 'progressAnimator')
  i908.progressTweenDuration = i909[11]
  i908.useFirstCubeScaleConfig = !!i909[12]
  i908.firstCubeScalePercent = i909[13]
  return i908
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.ParticleSystem"] = function (request, data, root) {
  var i912 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.ParticleSystem' )
  var i913 = data
  i912.main = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.MainModule', i913[0], i912.main)
  i912.colorBySpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorBySpeedModule', i913[1], i912.colorBySpeed)
  i912.colorOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorOverLifetimeModule', i913[2], i912.colorOverLifetime)
  i912.emission = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.EmissionModule', i913[3], i912.emission)
  i912.rotationBySpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationBySpeedModule', i913[4], i912.rotationBySpeed)
  i912.rotationOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationOverLifetimeModule', i913[5], i912.rotationOverLifetime)
  i912.shape = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ShapeModule', i913[6], i912.shape)
  i912.sizeBySpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeBySpeedModule', i913[7], i912.sizeBySpeed)
  i912.sizeOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeOverLifetimeModule', i913[8], i912.sizeOverLifetime)
  i912.textureSheetAnimation = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.TextureSheetAnimationModule', i913[9], i912.textureSheetAnimation)
  i912.velocityOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.VelocityOverLifetimeModule', i913[10], i912.velocityOverLifetime)
  i912.noise = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.NoiseModule', i913[11], i912.noise)
  i912.inheritVelocity = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.InheritVelocityModule', i913[12], i912.inheritVelocity)
  i912.forceOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ForceOverLifetimeModule', i913[13], i912.forceOverLifetime)
  i912.limitVelocityOverLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemModules.LimitVelocityOverLifetimeModule', i913[14], i912.limitVelocityOverLifetime)
  i912.useAutoRandomSeed = !!i913[15]
  i912.randomSeed = i913[16]
  return i912
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.MainModule"] = function (request, data, root) {
  var i914 = root || new pc.ParticleSystemMain()
  var i915 = data
  i914.duration = i915[0]
  i914.loop = !!i915[1]
  i914.prewarm = !!i915[2]
  i914.startDelay = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[3], i914.startDelay)
  i914.startLifetime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[4], i914.startLifetime)
  i914.startSpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[5], i914.startSpeed)
  i914.startSize3D = !!i915[6]
  i914.startSizeX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[7], i914.startSizeX)
  i914.startSizeY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[8], i914.startSizeY)
  i914.startSizeZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[9], i914.startSizeZ)
  i914.startRotation3D = !!i915[10]
  i914.startRotationX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[11], i914.startRotationX)
  i914.startRotationY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[12], i914.startRotationY)
  i914.startRotationZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[13], i914.startRotationZ)
  i914.startColor = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxGradient', i915[14], i914.startColor)
  i914.gravityModifier = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i915[15], i914.gravityModifier)
  i914.simulationSpace = i915[16]
  request.r(i915[17], i915[18], 0, i914, 'customSimulationSpace')
  i914.simulationSpeed = i915[19]
  i914.useUnscaledTime = !!i915[20]
  i914.scalingMode = i915[21]
  i914.playOnAwake = !!i915[22]
  i914.maxParticles = i915[23]
  i914.emitterVelocityMode = i915[24]
  i914.stopAction = i915[25]
  return i914
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve"] = function (request, data, root) {
  var i916 = root || new pc.MinMaxCurve()
  var i917 = data
  i916.mode = i917[0]
  i916.curveMin = new pc.AnimationCurve( { keys_flow: i917[1] } )
  i916.curveMax = new pc.AnimationCurve( { keys_flow: i917[2] } )
  i916.curveMultiplier = i917[3]
  i916.constantMin = i917[4]
  i916.constantMax = i917[5]
  return i916
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxGradient"] = function (request, data, root) {
  var i918 = root || new pc.MinMaxGradient()
  var i919 = data
  i918.mode = i919[0]
  i918.gradientMin = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Gradient', i919[1], i918.gradientMin)
  i918.gradientMax = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Gradient', i919[2], i918.gradientMax)
  i918.colorMin = new pc.Color(i919[3], i919[4], i919[5], i919[6])
  i918.colorMax = new pc.Color(i919[7], i919[8], i919[9], i919[10])
  return i918
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Gradient"] = function (request, data, root) {
  var i920 = root || request.c( 'Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Gradient' )
  var i921 = data
  i920.mode = i921[0]
  var i923 = i921[1]
  var i922 = []
  for(var i = 0; i < i923.length; i += 1) {
    i922.push( request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientColorKey', i923[i + 0]) );
  }
  i920.colorKeys = i922
  var i925 = i921[2]
  var i924 = []
  for(var i = 0; i < i925.length; i += 1) {
    i924.push( request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientAlphaKey', i925[i + 0]) );
  }
  i920.alphaKeys = i924
  return i920
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorBySpeedModule"] = function (request, data, root) {
  var i926 = root || new pc.ParticleSystemColorBySpeed()
  var i927 = data
  i926.enabled = !!i927[0]
  i926.color = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxGradient', i927[1], i926.color)
  i926.range = new pc.Vec2( i927[2], i927[3] )
  return i926
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientColorKey"] = function (request, data, root) {
  var i930 = root || request.c( 'Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientColorKey' )
  var i931 = data
  i930.color = new pc.Color(i931[0], i931[1], i931[2], i931[3])
  i930.time = i931[4]
  return i930
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientAlphaKey"] = function (request, data, root) {
  var i934 = root || request.c( 'Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientAlphaKey' )
  var i935 = data
  i934.alpha = i935[0]
  i934.time = i935[1]
  return i934
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorOverLifetimeModule"] = function (request, data, root) {
  var i936 = root || new pc.ParticleSystemColorOverLifetime()
  var i937 = data
  i936.enabled = !!i937[0]
  i936.color = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxGradient', i937[1], i936.color)
  return i936
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.EmissionModule"] = function (request, data, root) {
  var i938 = root || new pc.ParticleSystemEmitter()
  var i939 = data
  i938.enabled = !!i939[0]
  i938.rateOverTime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i939[1], i938.rateOverTime)
  i938.rateOverDistance = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i939[2], i938.rateOverDistance)
  var i941 = i939[3]
  var i940 = []
  for(var i = 0; i < i941.length; i += 1) {
    i940.push( request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Burst', i941[i + 0]) );
  }
  i938.bursts = i940
  return i938
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Burst"] = function (request, data, root) {
  var i944 = root || new pc.ParticleSystemBurst()
  var i945 = data
  i944.count = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i945[0], i944.count)
  i944.cycleCount = i945[1]
  i944.minCount = i945[2]
  i944.maxCount = i945[3]
  i944.repeatInterval = i945[4]
  i944.time = i945[5]
  return i944
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationBySpeedModule"] = function (request, data, root) {
  var i946 = root || new pc.ParticleSystemRotationBySpeed()
  var i947 = data
  i946.enabled = !!i947[0]
  i946.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i947[1], i946.x)
  i946.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i947[2], i946.y)
  i946.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i947[3], i946.z)
  i946.separateAxes = !!i947[4]
  i946.range = new pc.Vec2( i947[5], i947[6] )
  return i946
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationOverLifetimeModule"] = function (request, data, root) {
  var i948 = root || new pc.ParticleSystemRotationOverLifetime()
  var i949 = data
  i948.enabled = !!i949[0]
  i948.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i949[1], i948.x)
  i948.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i949[2], i948.y)
  i948.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i949[3], i948.z)
  i948.separateAxes = !!i949[4]
  return i948
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ShapeModule"] = function (request, data, root) {
  var i950 = root || new pc.ParticleSystemShape()
  var i951 = data
  i950.enabled = !!i951[0]
  i950.shapeType = i951[1]
  i950.randomDirectionAmount = i951[2]
  i950.sphericalDirectionAmount = i951[3]
  i950.randomPositionAmount = i951[4]
  i950.alignToDirection = !!i951[5]
  i950.radius = i951[6]
  i950.radiusMode = i951[7]
  i950.radiusSpread = i951[8]
  i950.radiusSpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i951[9], i950.radiusSpeed)
  i950.radiusThickness = i951[10]
  i950.angle = i951[11]
  i950.length = i951[12]
  i950.boxThickness = new pc.Vec3( i951[13], i951[14], i951[15] )
  i950.meshShapeType = i951[16]
  request.r(i951[17], i951[18], 0, i950, 'mesh')
  request.r(i951[19], i951[20], 0, i950, 'meshRenderer')
  request.r(i951[21], i951[22], 0, i950, 'skinnedMeshRenderer')
  i950.useMeshMaterialIndex = !!i951[23]
  i950.meshMaterialIndex = i951[24]
  i950.useMeshColors = !!i951[25]
  i950.normalOffset = i951[26]
  i950.arc = i951[27]
  i950.arcMode = i951[28]
  i950.arcSpread = i951[29]
  i950.arcSpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i951[30], i950.arcSpeed)
  i950.donutRadius = i951[31]
  i950.position = new pc.Vec3( i951[32], i951[33], i951[34] )
  i950.rotation = new pc.Vec3( i951[35], i951[36], i951[37] )
  i950.scale = new pc.Vec3( i951[38], i951[39], i951[40] )
  return i950
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeBySpeedModule"] = function (request, data, root) {
  var i952 = root || new pc.ParticleSystemSizeBySpeed()
  var i953 = data
  i952.enabled = !!i953[0]
  i952.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i953[1], i952.x)
  i952.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i953[2], i952.y)
  i952.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i953[3], i952.z)
  i952.separateAxes = !!i953[4]
  i952.range = new pc.Vec2( i953[5], i953[6] )
  return i952
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeOverLifetimeModule"] = function (request, data, root) {
  var i954 = root || new pc.ParticleSystemSizeOverLifetime()
  var i955 = data
  i954.enabled = !!i955[0]
  i954.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i955[1], i954.x)
  i954.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i955[2], i954.y)
  i954.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i955[3], i954.z)
  i954.separateAxes = !!i955[4]
  return i954
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.TextureSheetAnimationModule"] = function (request, data, root) {
  var i956 = root || new pc.ParticleSystemTextureSheetAnimation()
  var i957 = data
  i956.enabled = !!i957[0]
  i956.mode = i957[1]
  i956.animation = i957[2]
  i956.numTilesX = i957[3]
  i956.numTilesY = i957[4]
  i956.useRandomRow = !!i957[5]
  i956.frameOverTime = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i957[6], i956.frameOverTime)
  i956.startFrame = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i957[7], i956.startFrame)
  i956.cycleCount = i957[8]
  i956.rowIndex = i957[9]
  i956.flipU = i957[10]
  i956.flipV = i957[11]
  i956.spriteCount = i957[12]
  var i959 = i957[13]
  var i958 = []
  for(var i = 0; i < i959.length; i += 2) {
  request.r(i959[i + 0], i959[i + 1], 2, i958, '')
  }
  i956.sprites = i958
  return i956
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.VelocityOverLifetimeModule"] = function (request, data, root) {
  var i962 = root || new pc.ParticleSystemVelocityOverLifetime()
  var i963 = data
  i962.enabled = !!i963[0]
  i962.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[1], i962.x)
  i962.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[2], i962.y)
  i962.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[3], i962.z)
  i962.radial = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[4], i962.radial)
  i962.speedModifier = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[5], i962.speedModifier)
  i962.space = i963[6]
  i962.orbitalX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[7], i962.orbitalX)
  i962.orbitalY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[8], i962.orbitalY)
  i962.orbitalZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[9], i962.orbitalZ)
  i962.orbitalOffsetX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[10], i962.orbitalOffsetX)
  i962.orbitalOffsetY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[11], i962.orbitalOffsetY)
  i962.orbitalOffsetZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i963[12], i962.orbitalOffsetZ)
  return i962
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.NoiseModule"] = function (request, data, root) {
  var i964 = root || new pc.ParticleSystemNoise()
  var i965 = data
  i964.enabled = !!i965[0]
  i964.separateAxes = !!i965[1]
  i964.strengthX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[2], i964.strengthX)
  i964.strengthY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[3], i964.strengthY)
  i964.strengthZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[4], i964.strengthZ)
  i964.frequency = i965[5]
  i964.damping = !!i965[6]
  i964.octaveCount = i965[7]
  i964.octaveMultiplier = i965[8]
  i964.octaveScale = i965[9]
  i964.quality = i965[10]
  i964.scrollSpeed = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[11], i964.scrollSpeed)
  i964.scrollSpeedMultiplier = i965[12]
  i964.remapEnabled = !!i965[13]
  i964.remapX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[14], i964.remapX)
  i964.remapY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[15], i964.remapY)
  i964.remapZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[16], i964.remapZ)
  i964.positionAmount = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[17], i964.positionAmount)
  i964.rotationAmount = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[18], i964.rotationAmount)
  i964.sizeAmount = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i965[19], i964.sizeAmount)
  return i964
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.InheritVelocityModule"] = function (request, data, root) {
  var i966 = root || new pc.ParticleSystemInheritVelocity()
  var i967 = data
  i966.enabled = !!i967[0]
  i966.mode = i967[1]
  i966.curve = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i967[2], i966.curve)
  return i966
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ForceOverLifetimeModule"] = function (request, data, root) {
  var i968 = root || new pc.ParticleSystemForceOverLifetime()
  var i969 = data
  i968.enabled = !!i969[0]
  i968.x = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i969[1], i968.x)
  i968.y = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i969[2], i968.y)
  i968.z = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i969[3], i968.z)
  i968.space = i969[4]
  i968.randomized = !!i969[5]
  return i968
}

Deserializers["Luna.Unity.DTO.UnityEngine.ParticleSystemModules.LimitVelocityOverLifetimeModule"] = function (request, data, root) {
  var i970 = root || new pc.ParticleSystemLimitVelocityOverLifetime()
  var i971 = data
  i970.enabled = !!i971[0]
  i970.limit = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i971[1], i970.limit)
  i970.limitX = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i971[2], i970.limitX)
  i970.limitY = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i971[3], i970.limitY)
  i970.limitZ = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i971[4], i970.limitZ)
  i970.dampen = i971[5]
  i970.separateAxes = !!i971[6]
  i970.space = i971[7]
  i970.drag = request.d('Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve', i971[8], i970.drag)
  i970.multiplyDragByParticleSize = !!i971[9]
  i970.multiplyDragByParticleVelocity = !!i971[10]
  return i970
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.ParticleSystemRenderer"] = function (request, data, root) {
  var i972 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.ParticleSystemRenderer' )
  var i973 = data
  request.r(i973[0], i973[1], 0, i972, 'mesh')
  i972.meshCount = i973[2]
  i972.activeVertexStreamsCount = i973[3]
  i972.alignment = i973[4]
  i972.renderMode = i973[5]
  i972.sortMode = i973[6]
  i972.lengthScale = i973[7]
  i972.velocityScale = i973[8]
  i972.cameraVelocityScale = i973[9]
  i972.normalDirection = i973[10]
  i972.sortingFudge = i973[11]
  i972.minParticleSize = i973[12]
  i972.maxParticleSize = i973[13]
  i972.pivot = new pc.Vec3( i973[14], i973[15], i973[16] )
  request.r(i973[17], i973[18], 0, i972, 'trailMaterial')
  i972.applyActiveColorSpace = !!i973[19]
  i972.enabled = !!i973[20]
  request.r(i973[21], i973[22], 0, i972, 'sharedMaterial')
  var i975 = i973[23]
  var i974 = []
  for(var i = 0; i < i975.length; i += 2) {
  request.r(i975[i + 0], i975[i + 1], 2, i974, '')
  }
  i972.sharedMaterials = i974
  i972.receiveShadows = !!i973[24]
  i972.shadowCastingMode = i973[25]
  i972.sortingLayerID = i973[26]
  i972.sortingOrder = i973[27]
  i972.lightmapIndex = i973[28]
  i972.lightmapSceneIndex = i973[29]
  i972.lightmapScaleOffset = new pc.Vec4( i973[30], i973[31], i973[32], i973[33] )
  i972.lightProbeUsage = i973[34]
  i972.reflectionProbeUsage = i973[35]
  return i972
}

Deserializers["KeyAnim"] = function (request, data, root) {
  var i976 = root || request.c( 'KeyAnim' )
  var i977 = data
  var i979 = i977[0]
  var i978 = []
  for(var i = 0; i < i979.length; i += 2) {
  request.r(i979[i + 0], i979[i + 1], 2, i978, '')
  }
  i976.keyRenderers = i978
  return i976
}

Deserializers["ConveyorPortal"] = function (request, data, root) {
  var i980 = root || request.c( 'ConveyorPortal' )
  var i981 = data
  request.r(i981[0], i981[1], 0, i980, 'exitPoint')
  request.r(i981[2], i981[3], 0, i980, 'arrow')
  i980.exitDistance = i981[4]
  i980.gizmoLength = i981[5]
  return i980
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.SphereCollider"] = function (request, data, root) {
  var i982 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.SphereCollider' )
  var i983 = data
  i982.center = new pc.Vec3( i983[0], i983[1], i983[2] )
  i982.radius = i983[3]
  i982.enabled = !!i983[4]
  i982.isTrigger = !!i983[5]
  request.r(i983[6], i983[7], 0, i982, 'material')
  return i982
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Rigidbody"] = function (request, data, root) {
  var i984 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Rigidbody' )
  var i985 = data
  i984.mass = i985[0]
  i984.drag = i985[1]
  i984.angularDrag = i985[2]
  i984.useGravity = !!i985[3]
  i984.isKinematic = !!i985[4]
  i984.constraints = i985[5]
  i984.maxAngularVelocity = i985[6]
  i984.collisionDetectionMode = i985[7]
  i984.interpolation = i985[8]
  return i984
}

Deserializers["Cube"] = function (request, data, root) {
  var i986 = root || request.c( 'Cube' )
  var i987 = data
  request.r(i987[0], i987[1], 0, i986, 'Trans')
  request.r(i987[2], i987[3], 0, i986, 'cubeMovement')
  request.r(i987[4], i987[5], 0, i986, 'cubeDeliveryHandler')
  request.r(i987[6], i987[7], 0, i986, 'cubeVisual')
  return i986
}

Deserializers["CubeMovement"] = function (request, data, root) {
  var i988 = root || request.c( 'CubeMovement' )
  var i989 = data
  request.r(i989[0], i989[1], 0, i988, 'config')
  request.r(i989[2], i989[3], 0, i988, 'rb')
  return i988
}

Deserializers["CubeDeliveryHandler"] = function (request, data, root) {
  var i990 = root || request.c( 'CubeDeliveryHandler' )
  var i991 = data
  request.r(i991[0], i991[1], 0, i990, 'Trans')
  i990.config = request.d('CubeDeliveryConfig', i991[2], i990.config)
  return i990
}

Deserializers["CubeDeliveryConfig"] = function (request, data, root) {
  var i992 = root || request.c( 'CubeDeliveryConfig' )
  var i993 = data
  i992.Speed = i993[0]
  i992.EaseType = i993[1]
  i992.Height = i993[2]
  return i992
}

Deserializers["CubeVisual"] = function (request, data, root) {
  var i994 = root || request.c( 'CubeVisual' )
  var i995 = data
  request.r(i995[0], i995[1], 0, i994, 'cubeRenderer')
  return i994
}

Deserializers["AnimCube"] = function (request, data, root) {
  var i996 = root || request.c( 'AnimCube' )
  var i997 = data
  request.r(i997[0], i997[1], 0, i996, 'Trans')
  request.r(i997[2], i997[3], 0, i996, 'cubeDeliveryHandler')
  request.r(i997[4], i997[5], 0, i996, 'cubeVisual')
  return i996
}

Deserializers["Carrier"] = function (request, data, root) {
  var i998 = root || request.c( 'Carrier' )
  var i999 = data
  request.r(i999[0], i999[1], 0, i998, 'Trans')
  request.r(i999[2], i999[3], 0, i998, 'blockLayout')
  request.r(i999[4], i999[5], 0, i998, 'hiddenVisualRoot')
  request.r(i999[6], i999[7], 0, i998, 'mechanicVisualConfig')
  request.r(i999[8], i999[9], 0, i998, 'linkedBlockVisualConfig')
  var i1001 = i999[10]
  var i1000 = []
  for(var i = 0; i < i1001.length; i += 2) {
  request.r(i1001[i + 0], i1001[i + 1], 2, i1000, '')
  }
  i998.specialColorReceiverCarrierMeshRenderer = i1000
  request.r(i999[11], i999[12], 0, i998, 'pivot')
  i998.pickupDistanceOffset = i999[13]
  return i998
}

Deserializers["CarrierBlockLayout"] = function (request, data, root) {
  var i1004 = root || request.c( 'CarrierBlockLayout' )
  var i1005 = data
  request.r(i1005[0], i1005[1], 0, i1004, 'blockRoot')
  request.r(i1005[2], i1005[3], 0, i1004, 'blockSlotPrefab')
  i1004.spacing = i1005[4]
  i1004.paddingTop = i1005[5]
  i1004.paddingBottom = i1005[6]
  i1004.paddingLeft = i1005[7]
  i1004.paddingRight = i1005[8]
  i1004.childAlignment = i1005[9]
  i1004.layoutArea = new pc.Vec2( i1005[10], i1005[11] )
  var i1007 = i1005[12]
  var i1006 = new (System.Collections.Generic.List$1(Bridge.ns('Block')))
  for(var i = 0; i < i1007.length; i += 2) {
  request.r(i1007[i + 0], i1007[i + 1], 1, i1006, '')
  }
  i1004.blocks = i1006
  return i1004
}

Deserializers["CarrierSpawnEffect"] = function (request, data, root) {
  var i1010 = root || request.c( 'CarrierSpawnEffect' )
  var i1011 = data
  request.r(i1011[0], i1011[1], 0, i1010, 'spawnVfxPrefab')
  i1010.duration = i1011[2]
  return i1010
}

Deserializers["ContainerMechanic"] = function (request, data, root) {
  var i1012 = root || request.c( 'ContainerMechanic' )
  var i1013 = data
  i1012.containerId = i1013[0]
  i1012.isOpen = !!i1013[1]
  i1012.unlockColor = i1013[2]
  var i1015 = i1013[3]
  var i1014 = new (System.Collections.Generic.List$1(Bridge.ns('CarrierContainerMember')))
  for(var i = 0; i < i1015.length; i += 2) {
  request.r(i1015[i + 0], i1015[i + 1], 1, i1014, '')
  }
  i1012.carriers = i1014
  request.r(i1013[4], i1013[5], 0, i1012, 'giftBoxVisual1X')
  request.r(i1013[6], i1013[7], 0, i1012, 'giftBoxVisual2X')
  request.r(i1013[8], i1013[9], 0, i1012, 'giftBoxVisual3X')
  request.r(i1013[10], i1013[11], 0, i1012, 'ribbonParticle')
  request.r(i1013[12], i1013[13], 0, i1012, 'keyAnimator')
  var i1017 = i1013[14]
  var i1016 = []
  for(var i = 0; i < i1017.length; i += 2) {
  request.r(i1017[i + 0], i1017[i + 1], 2, i1016, '')
  }
  i1012.particleSystems = i1016
  i1012.delayVfx = i1013[15]
  return i1012
}

Deserializers["Key3DCodeAnimator"] = function (request, data, root) {
  var i1022 = root || request.c( 'Key3DCodeAnimator' )
  var i1023 = data
  request.r(i1023[0], i1023[1], 0, i1022, 'rootTransform')
  request.r(i1023[2], i1023[3], 0, i1022, 'scissorsL')
  request.r(i1023[4], i1023[5], 0, i1022, 'scissorsR')
  i1022.scaleMultiplier = i1023[6]
  return i1022
}

Deserializers["GiftBoxVisual"] = function (request, data, root) {
  var i1024 = root || request.c( 'GiftBoxVisual' )
  var i1025 = data
  request.r(i1025[0], i1025[1], 0, i1024, 'giftBoxRenderer')
  request.r(i1025[2], i1025[3], 0, i1024, 'lidRenderer')
  var i1027 = i1025[4]
  var i1026 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.MeshRenderer')))
  for(var i = 0; i < i1027.length; i += 2) {
  request.r(i1027[i + 0], i1027[i + 1], 1, i1026, '')
  }
  i1024.ribbonRenderers = i1026
  i1024.lidDuration = i1025[5]
  i1024.lidEase = i1025[6]
  i1024.giftBoxDuration = i1025[7]
  i1024.giftBoxEase = i1025[8]
  return i1024
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.MeshCollider"] = function (request, data, root) {
  var i1030 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.MeshCollider' )
  var i1031 = data
  request.r(i1031[0], i1031[1], 0, i1030, 'sharedMesh')
  i1030.convex = !!i1031[2]
  i1030.enabled = !!i1031[3]
  i1030.isTrigger = !!i1031[4]
  request.r(i1031[5], i1031[6], 0, i1030, 'material')
  return i1030
}

Deserializers["Spawner"] = function (request, data, root) {
  var i1032 = root || request.c( 'Spawner' )
  var i1033 = data
  request.r(i1033[0], i1033[1], 0, i1032, 'Trans')
  request.r(i1033[2], i1033[3], 0, i1032, 'blockPrefab')
  request.r(i1033[4], i1033[5], 0, i1032, 'container')
  request.r(i1033[6], i1033[7], 0, i1032, 'centerTrans')
  request.r(i1033[8], i1033[9], 0, i1032, 'remainingBlockCount')
  request.r(i1033[10], i1033[11], 0, i1032, 'remainingColorMesh')
  request.r(i1033[12], i1033[13], 0, i1032, 'remainingSlimeMesh')
  request.r(i1033[14], i1033[15], 0, i1032, 'remainingSlimeAnimator')
  request.r(i1033[16], i1033[17], 0, i1032, 'spawnAnimator')
  request.r(i1033[18], i1033[19], 0, i1032, 'pivot')
  i1032.pickupDistanceOffset = i1033[20]
  return i1032
}

Deserializers["SpawnerBlockAnimation"] = function (request, data, root) {
  var i1034 = root || request.c( 'SpawnerBlockAnimation' )
  var i1035 = data
  request.r(i1035[0], i1035[1], 0, i1034, 'startPoint')
  i1034.staggerDelay = i1035[2]
  i1034.flightHeight = i1035[3]
  i1034.customDuration = i1035[4]
  return i1034
}

Deserializers["SpawnerRemainingSlimeAnimator"] = function (request, data, root) {
  var i1036 = root || request.c( 'SpawnerRemainingSlimeAnimator' )
  var i1037 = data
  request.r(i1037[0], i1037[1], 0, i1036, 'targetTransform')
  i1036.scaleDuration = i1037[2]
  i1036.scaleEase = i1037[3]
  i1036.enablePulseOnScaleUp = !!i1037[4]
  i1036.pulseScaleAmount = i1037[5]
  i1036.pulseDuration = i1037[6]
  i1036.pulseEase = i1037[7]
  return i1036
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.RectTransform"] = function (request, data, root) {
  var i1038 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.RectTransform' )
  var i1039 = data
  i1038.pivot = new pc.Vec2( i1039[0], i1039[1] )
  i1038.anchorMin = new pc.Vec2( i1039[2], i1039[3] )
  i1038.anchorMax = new pc.Vec2( i1039[4], i1039[5] )
  i1038.sizeDelta = new pc.Vec2( i1039[6], i1039[7] )
  i1038.anchoredPosition3D = new pc.Vec3( i1039[8], i1039[9], i1039[10] )
  i1038.rotation = new pc.Quat(i1039[11], i1039[12], i1039[13], i1039[14])
  i1038.scale = new pc.Vec3( i1039[15], i1039[16], i1039[17] )
  return i1038
}

Deserializers["BlockLinkVisual"] = function (request, data, root) {
  var i1040 = root || request.c( 'BlockLinkVisual' )
  var i1041 = data
  i1040.linkWidth = i1041[0]
  i1040.lengthScale = i1041[1]
  i1040.lengthOffset = i1041[2]
  request.r(i1041[3], i1041[4], 0, i1040, 'vfxSplashDecayPrefab')
  i1040.vfxSpacing = i1041[5]
  return i1040
}

Deserializers["Luna.Unity.DTO.UnityEngine.Scene.Scene"] = function (request, data, root) {
  var i1042 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Scene.Scene' )
  var i1043 = data
  i1042.name = i1043[0]
  i1042.index = i1043[1]
  i1042.startup = !!i1043[2]
  return i1042
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Light"] = function (request, data, root) {
  var i1044 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Light' )
  var i1045 = data
  i1044.type = i1045[0]
  i1044.color = new pc.Color(i1045[1], i1045[2], i1045[3], i1045[4])
  i1044.cullingMask = i1045[5]
  i1044.intensity = i1045[6]
  i1044.range = i1045[7]
  i1044.spotAngle = i1045[8]
  i1044.shadows = i1045[9]
  i1044.shadowNormalBias = i1045[10]
  i1044.shadowBias = i1045[11]
  i1044.shadowStrength = i1045[12]
  i1044.shadowResolution = i1045[13]
  i1044.lightmapBakeType = i1045[14]
  i1044.renderMode = i1045[15]
  request.r(i1045[16], i1045[17], 0, i1044, 'cookie')
  i1044.cookieSize = i1045[18]
  i1044.shadowNearPlane = i1045[19]
  i1044.occlusionMaskChannel = i1045[20]
  i1044.isBaked = !!i1045[21]
  i1044.mixedLightingMode = i1045[22]
  i1044.enabled = !!i1045[23]
  return i1044
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Camera"] = function (request, data, root) {
  var i1046 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Camera' )
  var i1047 = data
  i1046.aspect = i1047[0]
  i1046.orthographic = !!i1047[1]
  i1046.orthographicSize = i1047[2]
  i1046.backgroundColor = new pc.Color(i1047[3], i1047[4], i1047[5], i1047[6])
  i1046.nearClipPlane = i1047[7]
  i1046.farClipPlane = i1047[8]
  i1046.fieldOfView = i1047[9]
  i1046.depth = i1047[10]
  i1046.clearFlags = i1047[11]
  i1046.cullingMask = i1047[12]
  i1046.rect = i1047[13]
  request.r(i1047[14], i1047[15], 0, i1046, 'targetTexture')
  i1046.usePhysicalProperties = !!i1047[16]
  i1046.focalLength = i1047[17]
  i1046.sensorSize = new pc.Vec2( i1047[18], i1047[19] )
  i1046.lensShift = new pc.Vec2( i1047[20], i1047[21] )
  i1046.gateFit = i1047[22]
  i1046.commandBufferCount = i1047[23]
  i1046.cameraType = i1047[24]
  i1046.enabled = !!i1047[25]
  return i1046
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Canvas"] = function (request, data, root) {
  var i1048 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Canvas' )
  var i1049 = data
  i1048.planeDistance = i1049[0]
  i1048.referencePixelsPerUnit = i1049[1]
  i1048.isFallbackOverlay = !!i1049[2]
  i1048.renderMode = i1049[3]
  i1048.renderOrder = i1049[4]
  i1048.sortingLayerName = i1049[5]
  i1048.sortingOrder = i1049[6]
  i1048.scaleFactor = i1049[7]
  request.r(i1049[8], i1049[9], 0, i1048, 'worldCamera')
  i1048.overrideSorting = !!i1049[10]
  i1048.pixelPerfect = !!i1049[11]
  i1048.targetDisplay = i1049[12]
  i1048.overridePixelPerfect = !!i1049[13]
  i1048.enabled = !!i1049[14]
  return i1048
}

Deserializers["UnityEngine.UI.CanvasScaler"] = function (request, data, root) {
  var i1050 = root || request.c( 'UnityEngine.UI.CanvasScaler' )
  var i1051 = data
  i1050.m_UiScaleMode = i1051[0]
  i1050.m_ReferencePixelsPerUnit = i1051[1]
  i1050.m_ScaleFactor = i1051[2]
  i1050.m_ReferenceResolution = new pc.Vec2( i1051[3], i1051[4] )
  i1050.m_ScreenMatchMode = i1051[5]
  i1050.m_MatchWidthOrHeight = i1051[6]
  i1050.m_PhysicalUnit = i1051[7]
  i1050.m_FallbackScreenDPI = i1051[8]
  i1050.m_DefaultSpriteDPI = i1051[9]
  i1050.m_DynamicPixelsPerUnit = i1051[10]
  i1050.m_PresetInfoIsWorld = !!i1051[11]
  return i1050
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.CanvasRenderer"] = function (request, data, root) {
  var i1052 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.CanvasRenderer' )
  var i1053 = data
  i1052.cullTransparentMesh = !!i1053[0]
  return i1052
}

Deserializers["UnityEngine.UI.Image"] = function (request, data, root) {
  var i1054 = root || request.c( 'UnityEngine.UI.Image' )
  var i1055 = data
  request.r(i1055[0], i1055[1], 0, i1054, 'm_Sprite')
  i1054.m_Type = i1055[2]
  i1054.m_PreserveAspect = !!i1055[3]
  i1054.m_FillCenter = !!i1055[4]
  i1054.m_FillMethod = i1055[5]
  i1054.m_FillAmount = i1055[6]
  i1054.m_FillClockwise = !!i1055[7]
  i1054.m_FillOrigin = i1055[8]
  i1054.m_UseSpriteMesh = !!i1055[9]
  i1054.m_PixelsPerUnitMultiplier = i1055[10]
  request.r(i1055[11], i1055[12], 0, i1054, 'm_Material')
  i1054.m_Maskable = !!i1055[13]
  i1054.m_Color = new pc.Color(i1055[14], i1055[15], i1055[16], i1055[17])
  i1054.m_RaycastTarget = !!i1055[18]
  i1054.m_RaycastPadding = new pc.Vec4( i1055[19], i1055[20], i1055[21], i1055[22] )
  return i1054
}

Deserializers["UnityEngine.Splines.SplineContainer"] = function (request, data, root) {
  var i1056 = root || request.c( 'UnityEngine.Splines.SplineContainer' )
  var i1057 = data
  i1056.m_Spline = request.d('UnityEngine.Splines.Spline', i1057[0], i1056.m_Spline)
  var i1059 = i1057[1]
  var i1058 = []
  for(var i = 0; i < i1059.length; i += 1) {
    i1058.push( request.d('UnityEngine.Splines.Spline', i1059[i + 0]) );
  }
  i1056.m_Splines = i1058
  i1056.m_Knots = request.d('UnityEngine.Splines.KnotLinkCollection', i1057[2], i1056.m_Knots)
  return i1056
}

Deserializers["UnityEngine.Splines.Spline"] = function (request, data, root) {
  var i1060 = root || request.c( 'UnityEngine.Splines.Spline' )
  var i1061 = data
  i1060.m_EditModeType = i1061[0]
  var i1063 = i1061[1]
  var i1062 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Splines.BezierKnot')))
  for(var i = 0; i < i1063.length; i += 1) {
    i1062.add(request.d('UnityEngine.Splines.BezierKnot', i1063[i + 0]));
  }
  i1060.m_Knots = i1062
  var i1065 = i1061[2]
  var i1064 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Splines.Spline+MetaData')))
  for(var i = 0; i < i1065.length; i += 1) {
    i1064.add(request.d('UnityEngine.Splines.Spline+MetaData', i1065[i + 0]));
  }
  i1060.m_MetaData = i1064
  i1060.m_Closed = !!i1061[3]
  return i1060
}

Deserializers["UnityEngine.Splines.BezierKnot"] = function (request, data, root) {
  var i1068 = root || request.c( 'UnityEngine.Splines.BezierKnot' )
  var i1069 = data
  i1068.Position = request.d('Unity.Mathematics.float3', i1069[0], i1068.Position)
  i1068.TangentIn = request.d('Unity.Mathematics.float3', i1069[1], i1068.TangentIn)
  i1068.TangentOut = request.d('Unity.Mathematics.float3', i1069[2], i1068.TangentOut)
  i1068.Rotation = request.d('Unity.Mathematics.quaternion', i1069[3], i1068.Rotation)
  return i1068
}

Deserializers["UnityEngine.Splines.Spline+MetaData"] = function (request, data, root) {
  var i1072 = root || request.c( 'UnityEngine.Splines.Spline+MetaData' )
  var i1073 = data
  i1072.Mode = i1073[0]
  i1072.Tension = i1073[1]
  return i1072
}

Deserializers["Unity.Mathematics.float3"] = function (request, data, root) {
  var i1076 = root || request.c( 'Unity.Mathematics.float3' )
  var i1077 = data
  i1076.x = i1077[0]
  i1076.y = i1077[1]
  i1076.z = i1077[2]
  return i1076
}

Deserializers["Unity.Mathematics.quaternion"] = function (request, data, root) {
  var i1078 = root || request.c( 'Unity.Mathematics.quaternion' )
  var i1079 = data
  i1078.value = request.d('Unity.Mathematics.float4', i1079[0], i1078.value)
  return i1078
}

Deserializers["Unity.Mathematics.float4"] = function (request, data, root) {
  var i1080 = root || request.c( 'Unity.Mathematics.float4' )
  var i1081 = data
  i1080.x = i1081[0]
  i1080.y = i1081[1]
  i1080.z = i1081[2]
  i1080.w = i1081[3]
  return i1080
}

Deserializers["UnityEngine.Splines.KnotLinkCollection"] = function (request, data, root) {
  var i1082 = root || request.c( 'UnityEngine.Splines.KnotLinkCollection' )
  var i1083 = data
  var i1085 = i1083[0]
  var i1084 = []
  for(var i = 0; i < i1085.length; i += 1) {
    i1084.push( request.d('UnityEngine.Splines.KnotLinkCollection+KnotLink', i1085[i + 0]) );
  }
  i1082.m_KnotsLink = i1084
  return i1082
}

Deserializers["UnityEngine.Splines.KnotLinkCollection+KnotLink"] = function (request, data, root) {
  var i1088 = root || request.c( 'UnityEngine.Splines.KnotLinkCollection+KnotLink' )
  var i1089 = data
  var i1091 = i1089[0]
  var i1090 = []
  for(var i = 0; i < i1091.length; i += 1) {
    i1090.push( request.d('UnityEngine.Splines.SplineKnotIndex', i1091[i + 0]) );
  }
  i1088.Knots = i1090
  return i1088
}

Deserializers["ConveyorManager"] = function (request, data, root) {
  var i1092 = root || request.c( 'ConveyorManager' )
  var i1093 = data
  request.r(i1093[0], i1093[1], 0, i1092, 'conveyorContainer')
  request.r(i1093[2], i1093[3], 0, i1092, 'splineInstantiate')
  request.r(i1093[4], i1093[5], 0, i1092, 'conveyorMeshBuilder')
  request.r(i1093[6], i1093[7], 0, i1092, 'conveyorCornerDetector')
  request.r(i1093[8], i1093[9], 0, i1092, 'conveyorPortalPrefab')
  request.r(i1093[10], i1093[11], 0, i1092, 'portalHolder')
  request.r(i1093[12], i1093[13], 0, i1092, 'conveyorRoot')
  return i1092
}

Deserializers["ConveyorCornerDetector"] = function (request, data, root) {
  var i1094 = root || request.c( 'ConveyorCornerDetector' )
  var i1095 = data
  request.r(i1095[0], i1095[1], 0, i1094, 'config')
  var i1097 = i1095[2]
  var i1096 = new (System.Collections.Generic.List$1(Bridge.ns('System.Single')))
  for(var i = 0; i < i1097.length; i += 1) {
    i1096.add(i1097[i + 0]);
  }
  i1094.cornerProgresses = i1096
  return i1094
}

Deserializers["ConveyorDeliverySystem"] = function (request, data, root) {
  var i1100 = root || request.c( 'ConveyorDeliverySystem' )
  var i1101 = data
  request.r(i1101[0], i1101[1], 0, i1100, 'conveyorManager')
  request.r(i1101[2], i1101[3], 0, i1100, 'conveyorMeshBuilder')
  request.r(i1101[4], i1101[5], 0, i1100, 'cubeConfig')
  i1100.spawnInterval = i1101[6]
  request.r(i1101[7], i1101[8], 0, i1100, 'spawnRoot')
  request.r(i1101[9], i1101[10], 0, i1100, 'conveyorSpawnPointConfig')
  var i1103 = i1101[11]
  var i1102 = new (System.Collections.Generic.List$1(Bridge.ns('Cube')))
  for(var i = 0; i < i1103.length; i += 2) {
  request.r(i1103[i + 0], i1103[i + 1], 1, i1102, '')
  }
  i1100.cachedMovers = i1102
  request.r(i1101[12], i1101[13], 0, i1100, 'conveyorSpeedBoostConfig')
  request.r(i1101[14], i1101[15], 0, i1100, 'conveyorCornerDetector')
  i1100.pickupThreshold = i1101[16]
  return i1100
}

Deserializers["UnityEngine.Splines.SplineInstantiate"] = function (request, data, root) {
  var i1106 = root || request.c( 'UnityEngine.Splines.SplineInstantiate' )
  var i1107 = data
  request.r(i1107[0], i1107[1], 0, i1106, 'm_Container')
  var i1109 = i1107[2]
  var i1108 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Splines.SplineInstantiate+InstantiableItem')))
  for(var i = 0; i < i1109.length; i += 1) {
    i1108.add(request.d('UnityEngine.Splines.SplineInstantiate+InstantiableItem', i1109[i + 0]));
  }
  i1106.m_ItemsToInstantiate = i1108
  i1106.m_Method = i1107[3]
  i1106.m_Space = i1107[4]
  i1106.m_Spacing = new pc.Vec2( i1107[5], i1107[6] )
  i1106.m_Up = i1107[7]
  i1106.m_Forward = i1107[8]
  i1106.m_PositionOffset = request.d('UnityEngine.Splines.SplineInstantiate+Vector3Offset', i1107[9], i1106.m_PositionOffset)
  i1106.m_RotationOffset = request.d('UnityEngine.Splines.SplineInstantiate+Vector3Offset', i1107[10], i1106.m_RotationOffset)
  i1106.m_ScaleOffset = request.d('UnityEngine.Splines.SplineInstantiate+Vector3Offset', i1107[11], i1106.m_ScaleOffset)
  var i1111 = i1107[12]
  var i1110 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.GameObject')))
  for(var i = 0; i < i1111.length; i += 2) {
  request.r(i1111[i + 0], i1111[i + 1], 1, i1110, '')
  }
  i1106.m_DeprecatedInstances = i1110
  i1106.m_AutoRefresh = !!i1107[13]
  i1106.m_Seed = i1107[14]
  return i1106
}

Deserializers["UnityEngine.Splines.SplineInstantiate+InstantiableItem"] = function (request, data, root) {
  var i1114 = root || request.c( 'UnityEngine.Splines.SplineInstantiate+InstantiableItem' )
  var i1115 = data
  request.r(i1115[0], i1115[1], 0, i1114, 'prefab')
  request.r(i1115[2], i1115[3], 0, i1114, 'Prefab')
  i1114.probability = i1115[4]
  i1114.Probability = i1115[5]
  return i1114
}

Deserializers["UnityEngine.Splines.SplineInstantiate+Vector3Offset"] = function (request, data, root) {
  var i1116 = root || request.c( 'UnityEngine.Splines.SplineInstantiate+Vector3Offset' )
  var i1117 = data
  i1116.setup = i1117[0]
  i1116.min = new pc.Vec3( i1117[1], i1117[2], i1117[3] )
  i1116.max = new pc.Vec3( i1117[4], i1117[5], i1117[6] )
  i1116.randomX = !!i1117[7]
  i1116.randomY = !!i1117[8]
  i1116.randomZ = !!i1117[9]
  i1116.space = i1117[10]
  return i1116
}

Deserializers["ConveyorMeshBuilder"] = function (request, data, root) {
  var i1120 = root || request.c( 'ConveyorMeshBuilder' )
  var i1121 = data
  i1120.roadWidth = i1121[0]
  i1120.roadThickness = i1121[1]
  i1120.sampleCount = i1121[2]
  request.r(i1121[3], i1121[4], 0, i1120, 'roadMaterial')
  request.r(i1121[5], i1121[6], 0, i1120, 'cubeMovementConfig')
  i1120.roadVisualSampleMultiplier = i1121[7]
  i1120.roadVisualSurfaceOffset = i1121[8]
  i1120.shaderAcceleration = i1121[9]
  i1120.railWidth = i1121[10]
  i1120.railHeight = i1121[11]
  i1120.railCornerRadius = i1121[12]
  i1120.railCornerSegments = i1121[13]
  i1120.railLightingFromTop = i1121[14]
  i1120.generateRoadCollider = !!i1121[15]
  i1120.roadColliderScaleMultiplier = i1121[16]
  return i1120
}

Deserializers["CarrierSystem"] = function (request, data, root) {
  var i1122 = root || request.c( 'CarrierSystem' )
  var i1123 = data
  request.r(i1123[0], i1123[1], 0, i1122, 'carrierSpawner')
  return i1122
}

Deserializers["CarrierSpawner"] = function (request, data, root) {
  var i1124 = root || request.c( 'CarrierSpawner' )
  var i1125 = data
  request.r(i1125[0], i1125[1], 0, i1124, 'carrierConfig')
  request.r(i1125[2], i1125[3], 0, i1124, 'spawnRoot')
  return i1124
}

Deserializers["CapacityManager"] = function (request, data, root) {
  var i1126 = root || request.c( 'CapacityManager' )
  var i1127 = data
  return i1126
}

Deserializers["GameConditionManager"] = function (request, data, root) {
  var i1128 = root || request.c( 'GameConditionManager' )
  var i1129 = data
  request.r(i1129[0], i1129[1], 0, i1128, 'config')
  return i1128
}

Deserializers["LevelManager"] = function (request, data, root) {
  var i1130 = root || request.c( 'LevelManager' )
  var i1131 = data
  i1130.IsTutorial = !!i1131[0]
  var i1133 = i1131[1]
  var i1132 = new (System.Collections.Generic.List$1(Bridge.ns('LevelData')))
  for(var i = 0; i < i1133.length; i += 2) {
  request.r(i1133[i + 0], i1133[i + 1], 1, i1132, '')
  }
  i1130.playableLevels = i1132
  i1130.startLevelIndex = i1131[2]
  i1130.loadOnStart = !!i1131[3]
  i1130.autoPlayNextLevel = !!i1131[4]
  i1130.loopLevelSequence = !!i1131[5]
  request.r(i1131[6], i1131[7], 0, i1130, 'levelEntryAnimConfig')
  request.r(i1131[8], i1131[9], 0, i1130, 'conveyorManager')
  request.r(i1131[10], i1131[11], 0, i1130, 'carrierSystem')
  request.r(i1131[12], i1131[13], 0, i1130, 'capacityManager')
  return i1130
}

Deserializers["CameraManager"] = function (request, data, root) {
  var i1136 = root || request.c( 'CameraManager' )
  var i1137 = data
  request.r(i1137[0], i1137[1], 0, i1136, 'mainCamera')
  request.r(i1137[2], i1137[3], 0, i1136, 'highlightCamera')
  return i1136
}

Deserializers["InputController"] = function (request, data, root) {
  var i1138 = root || request.c( 'InputController' )
  var i1139 = data
  i1138.clickableLayerMask = UnityEngine.LayerMask.FromIntegerValue( i1139[0] )
  return i1138
}

Deserializers["PoolManagerNew"] = function (request, data, root) {
  var i1140 = root || request.c( 'PoolManagerNew' )
  var i1141 = data
  return i1140
}

Deserializers["ConfigManager"] = function (request, data, root) {
  var i1142 = root || request.c( 'ConfigManager' )
  var i1143 = data
  request.r(i1143[0], i1143[1], 0, i1142, 'colorConfig')
  request.r(i1143[2], i1143[3], 0, i1142, 'cubeColorConfig')
  request.r(i1143[4], i1143[5], 0, i1142, 'cubeConfig')
  request.r(i1143[6], i1143[7], 0, i1142, 'specialColor')
  request.r(i1143[8], i1143[9], 0, i1142, 'carrierConfig')
  request.r(i1143[10], i1143[11], 0, i1142, 'catColorConfig')
  request.r(i1143[12], i1143[13], 0, i1142, 'animBlockConfig')
  request.r(i1143[14], i1143[15], 0, i1142, 'cubeMovementConfig')
  request.r(i1143[16], i1143[17], 0, i1142, 'stylizedColorConfig')
  request.r(i1143[18], i1143[19], 0, i1142, 'remainingColorConfig')
  return i1142
}

Deserializers["CustomTimeScaleGroup"] = function (request, data, root) {
  var i1144 = root || request.c( 'CustomTimeScaleGroup' )
  var i1145 = data
  var i1147 = i1145[0]
  var i1146 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.MonoBehaviour')))
  for(var i = 0; i < i1147.length; i += 2) {
  request.r(i1147[i + 0], i1147[i + 1], 1, i1146, '')
  }
  i1144.targets = i1146
  return i1144
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings"] = function (request, data, root) {
  var i1150 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.RenderSettings' )
  var i1151 = data
  i1150.ambientIntensity = i1151[0]
  i1150.reflectionIntensity = i1151[1]
  i1150.ambientMode = i1151[2]
  i1150.ambientLight = new pc.Color(i1151[3], i1151[4], i1151[5], i1151[6])
  i1150.ambientSkyColor = new pc.Color(i1151[7], i1151[8], i1151[9], i1151[10])
  i1150.ambientGroundColor = new pc.Color(i1151[11], i1151[12], i1151[13], i1151[14])
  i1150.ambientEquatorColor = new pc.Color(i1151[15], i1151[16], i1151[17], i1151[18])
  i1150.fogColor = new pc.Color(i1151[19], i1151[20], i1151[21], i1151[22])
  i1150.fogEndDistance = i1151[23]
  i1150.fogStartDistance = i1151[24]
  i1150.fogDensity = i1151[25]
  i1150.fog = !!i1151[26]
  request.r(i1151[27], i1151[28], 0, i1150, 'skybox')
  i1150.fogMode = i1151[29]
  var i1153 = i1151[30]
  var i1152 = []
  for(var i = 0; i < i1153.length; i += 1) {
    i1152.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap', i1153[i + 0]) );
  }
  i1150.lightmaps = i1152
  i1150.lightProbes = request.d('Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes', i1151[31], i1150.lightProbes)
  i1150.lightmapsMode = i1151[32]
  i1150.mixedBakeMode = i1151[33]
  i1150.environmentLightingMode = i1151[34]
  i1150.ambientProbe = new pc.SphericalHarmonicsL2(i1151[35])
  request.r(i1151[36], i1151[37], 0, i1150, 'customReflection')
  request.r(i1151[38], i1151[39], 0, i1150, 'defaultReflection')
  i1150.defaultReflectionMode = i1151[40]
  i1150.defaultReflectionResolution = i1151[41]
  i1150.sunLightObjectId = i1151[42]
  i1150.pixelLightCount = i1151[43]
  i1150.defaultReflectionHDR = !!i1151[44]
  i1150.hasLightDataAsset = !!i1151[45]
  i1150.hasManualGenerate = !!i1151[46]
  return i1150
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap"] = function (request, data, root) {
  var i1156 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap' )
  var i1157 = data
  request.r(i1157[0], i1157[1], 0, i1156, 'lightmapColor')
  request.r(i1157[2], i1157[3], 0, i1156, 'lightmapDirection')
  request.r(i1157[4], i1157[5], 0, i1156, 'shadowMask')
  return i1156
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes"] = function (request, data, root) {
  var i1158 = root || new UnityEngine.LightProbes()
  var i1159 = data
  return i1158
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.PhysicMaterial"] = function (request, data, root) {
  var i1166 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.PhysicMaterial' )
  var i1167 = data
  i1166.name = i1167[0]
  i1166.bounciness = i1167[1]
  i1166.dynamicFriction = i1167[2]
  i1166.staticFriction = i1167[3]
  i1166.frictionCombine = i1167[4]
  i1166.bounceCombine = i1167[5]
  return i1166
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader"] = function (request, data, root) {
  var i1168 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader' )
  var i1169 = data
  var i1171 = i1169[0]
  var i1170 = new (System.Collections.Generic.List$1(Bridge.ns('Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError')))
  for(var i = 0; i < i1171.length; i += 1) {
    i1170.add(request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError', i1171[i + 0]));
  }
  i1168.ShaderCompilationErrors = i1170
  i1168.name = i1169[1]
  i1168.guid = i1169[2]
  var i1173 = i1169[3]
  var i1172 = []
  for(var i = 0; i < i1173.length; i += 1) {
    i1172.push( i1173[i + 0] );
  }
  i1168.shaderDefinedKeywords = i1172
  var i1175 = i1169[4]
  var i1174 = []
  for(var i = 0; i < i1175.length; i += 1) {
    i1174.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass', i1175[i + 0]) );
  }
  i1168.passes = i1174
  var i1177 = i1169[5]
  var i1176 = []
  for(var i = 0; i < i1177.length; i += 1) {
    i1176.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass', i1177[i + 0]) );
  }
  i1168.usePasses = i1176
  var i1179 = i1169[6]
  var i1178 = []
  for(var i = 0; i < i1179.length; i += 1) {
    i1178.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue', i1179[i + 0]) );
  }
  i1168.defaultParameterValues = i1178
  request.r(i1169[7], i1169[8], 0, i1168, 'unityFallbackShader')
  i1168.readDepth = !!i1169[9]
  i1168.hasDepthOnlyPass = !!i1169[10]
  i1168.isCreatedByShaderGraph = !!i1169[11]
  i1168.disableBatching = !!i1169[12]
  i1168.compiled = !!i1169[13]
  return i1168
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError"] = function (request, data, root) {
  var i1182 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError' )
  var i1183 = data
  i1182.shaderName = i1183[0]
  i1182.errorMessage = i1183[1]
  return i1182
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass"] = function (request, data, root) {
  var i1188 = root || new pc.UnityShaderPass()
  var i1189 = data
  i1188.id = i1189[0]
  i1188.subShaderIndex = i1189[1]
  i1188.name = i1189[2]
  i1188.passType = i1189[3]
  i1188.grabPassTextureName = i1189[4]
  i1188.usePass = !!i1189[5]
  i1188.zTest = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[6], i1188.zTest)
  i1188.zWrite = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[7], i1188.zWrite)
  i1188.culling = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[8], i1188.culling)
  i1188.blending = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending', i1189[9], i1188.blending)
  i1188.alphaBlending = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending', i1189[10], i1188.alphaBlending)
  i1188.colorWriteMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[11], i1188.colorWriteMask)
  i1188.offsetUnits = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[12], i1188.offsetUnits)
  i1188.offsetFactor = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[13], i1188.offsetFactor)
  i1188.stencilRef = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[14], i1188.stencilRef)
  i1188.stencilReadMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[15], i1188.stencilReadMask)
  i1188.stencilWriteMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1189[16], i1188.stencilWriteMask)
  i1188.stencilOp = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i1189[17], i1188.stencilOp)
  i1188.stencilOpFront = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i1189[18], i1188.stencilOpFront)
  i1188.stencilOpBack = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i1189[19], i1188.stencilOpBack)
  var i1191 = i1189[20]
  var i1190 = []
  for(var i = 0; i < i1191.length; i += 1) {
    i1190.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag', i1191[i + 0]) );
  }
  i1188.tags = i1190
  var i1193 = i1189[21]
  var i1192 = []
  for(var i = 0; i < i1193.length; i += 1) {
    i1192.push( i1193[i + 0] );
  }
  i1188.passDefinedKeywords = i1192
  var i1195 = i1189[22]
  var i1194 = []
  for(var i = 0; i < i1195.length; i += 1) {
    i1194.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup', i1195[i + 0]) );
  }
  i1188.passDefinedKeywordGroups = i1194
  var i1197 = i1189[23]
  var i1196 = []
  for(var i = 0; i < i1197.length; i += 1) {
    i1196.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant', i1197[i + 0]) );
  }
  i1188.variants = i1196
  var i1199 = i1189[24]
  var i1198 = []
  for(var i = 0; i < i1199.length; i += 1) {
    i1198.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant', i1199[i + 0]) );
  }
  i1188.excludedVariants = i1198
  i1188.hasDepthReader = !!i1189[25]
  return i1188
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value"] = function (request, data, root) {
  var i1200 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value' )
  var i1201 = data
  i1200.val = i1201[0]
  i1200.name = i1201[1]
  return i1200
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending"] = function (request, data, root) {
  var i1202 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending' )
  var i1203 = data
  i1202.src = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1203[0], i1202.src)
  i1202.dst = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1203[1], i1202.dst)
  i1202.op = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1203[2], i1202.op)
  return i1202
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp"] = function (request, data, root) {
  var i1204 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp' )
  var i1205 = data
  i1204.pass = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1205[0], i1204.pass)
  i1204.fail = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1205[1], i1204.fail)
  i1204.zFail = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1205[2], i1204.zFail)
  i1204.comp = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i1205[3], i1204.comp)
  return i1204
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag"] = function (request, data, root) {
  var i1208 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag' )
  var i1209 = data
  i1208.name = i1209[0]
  i1208.value = i1209[1]
  return i1208
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup"] = function (request, data, root) {
  var i1212 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup' )
  var i1213 = data
  var i1215 = i1213[0]
  var i1214 = []
  for(var i = 0; i < i1215.length; i += 1) {
    i1214.push( i1215[i + 0] );
  }
  i1212.keywords = i1214
  i1212.hasDiscard = !!i1213[1]
  return i1212
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant"] = function (request, data, root) {
  var i1218 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant' )
  var i1219 = data
  i1218.passId = i1219[0]
  i1218.subShaderIndex = i1219[1]
  var i1221 = i1219[2]
  var i1220 = []
  for(var i = 0; i < i1221.length; i += 1) {
    i1220.push( i1221[i + 0] );
  }
  i1218.keywords = i1220
  i1218.vertexProgram = i1219[3]
  i1218.fragmentProgram = i1219[4]
  i1218.exportedForWebGl2 = !!i1219[5]
  i1218.readDepth = !!i1219[6]
  return i1218
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass"] = function (request, data, root) {
  var i1224 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass' )
  var i1225 = data
  request.r(i1225[0], i1225[1], 0, i1224, 'shader')
  i1224.pass = i1225[2]
  return i1224
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue"] = function (request, data, root) {
  var i1228 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue' )
  var i1229 = data
  i1228.name = i1229[0]
  i1228.type = i1229[1]
  i1228.value = new pc.Vec4( i1229[2], i1229[3], i1229[4], i1229[5] )
  i1228.textureValue = i1229[6]
  i1228.shaderPropertyFlag = i1229[7]
  return i1228
}

Deserializers["Luna.Unity.DTO.UnityEngine.Textures.Sprite"] = function (request, data, root) {
  var i1230 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Textures.Sprite' )
  var i1231 = data
  i1230.name = i1231[0]
  request.r(i1231[1], i1231[2], 0, i1230, 'texture')
  i1230.aabb = i1231[3]
  i1230.vertices = i1231[4]
  i1230.triangles = i1231[5]
  i1230.textureRect = UnityEngine.Rect.MinMaxRect(i1231[6], i1231[7], i1231[8], i1231[9])
  i1230.packedRect = UnityEngine.Rect.MinMaxRect(i1231[10], i1231[11], i1231[12], i1231[13])
  i1230.border = new pc.Vec4( i1231[14], i1231[15], i1231[16], i1231[17] )
  i1230.transparency = i1231[18]
  i1230.bounds = i1231[19]
  i1230.pixelsPerUnit = i1231[20]
  i1230.textureWidth = i1231[21]
  i1230.textureHeight = i1231[22]
  i1230.nativeSize = new pc.Vec2( i1231[23], i1231[24] )
  i1230.pivot = new pc.Vec2( i1231[25], i1231[26] )
  i1230.textureRectOffset = new pc.Vec2( i1231[27], i1231[28] )
  return i1230
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip"] = function (request, data, root) {
  var i1232 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip' )
  var i1233 = data
  i1232.name = i1233[0]
  i1232.wrapMode = i1233[1]
  i1232.isLooping = !!i1233[2]
  i1232.length = i1233[3]
  var i1235 = i1233[4]
  var i1234 = []
  for(var i = 0; i < i1235.length; i += 1) {
    i1234.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve', i1235[i + 0]) );
  }
  i1232.curves = i1234
  var i1237 = i1233[5]
  var i1236 = []
  for(var i = 0; i < i1237.length; i += 1) {
    i1236.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationEvent', i1237[i + 0]) );
  }
  i1232.events = i1236
  i1232.halfPrecision = !!i1233[6]
  i1232._frameRate = i1233[7]
  i1232.localBounds = request.d('Luna.Unity.DTO.UnityEngine.Animation.Data.Bounds', i1233[8], i1232.localBounds)
  i1232.hasMuscleCurves = !!i1233[9]
  var i1239 = i1233[10]
  var i1238 = []
  for(var i = 0; i < i1239.length; i += 1) {
    i1238.push( i1239[i + 0] );
  }
  i1232.clipMuscleConstant = i1238
  i1232.clipBindingConstant = request.d('Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip+AnimationClipBindingConstant', i1233[11], i1232.clipBindingConstant)
  return i1232
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve"] = function (request, data, root) {
  var i1242 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve' )
  var i1243 = data
  i1242.path = i1243[0]
  i1242.hash = i1243[1]
  i1242.componentType = i1243[2]
  i1242.property = i1243[3]
  i1242.keys = i1243[4]
  var i1245 = i1243[5]
  var i1244 = []
  for(var i = 0; i < i1245.length; i += 1) {
    i1244.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve+ObjectReferenceKey', i1245[i + 0]) );
  }
  i1242.objectReferenceKeys = i1244
  return i1242
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve+ObjectReferenceKey"] = function (request, data, root) {
  var i1248 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve+ObjectReferenceKey' )
  var i1249 = data
  i1248.time = i1249[0]
  request.r(i1249[1], i1249[2], 0, i1248, 'value')
  return i1248
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationEvent"] = function (request, data, root) {
  var i1252 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationEvent' )
  var i1253 = data
  i1252.functionName = i1253[0]
  i1252.floatParameter = i1253[1]
  i1252.intParameter = i1253[2]
  i1252.stringParameter = i1253[3]
  request.r(i1253[4], i1253[5], 0, i1252, 'objectReferenceParameter')
  i1252.time = i1253[6]
  return i1252
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.Bounds"] = function (request, data, root) {
  var i1254 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.Bounds' )
  var i1255 = data
  i1254.center = new pc.Vec3( i1255[0], i1255[1], i1255[2] )
  i1254.extends = new pc.Vec3( i1255[3], i1255[4], i1255[5] )
  return i1254
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip+AnimationClipBindingConstant"] = function (request, data, root) {
  var i1258 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip+AnimationClipBindingConstant' )
  var i1259 = data
  var i1261 = i1259[0]
  var i1260 = []
  for(var i = 0; i < i1261.length; i += 1) {
    i1260.push( i1261[i + 0] );
  }
  i1258.genericBindings = i1260
  var i1263 = i1259[1]
  var i1262 = []
  for(var i = 0; i < i1263.length; i += 1) {
    i1262.push( i1263[i + 0] );
  }
  i1258.pptrCurveMapping = i1262
  return i1258
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorController"] = function (request, data, root) {
  var i1264 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorController' )
  var i1265 = data
  i1264.name = i1265[0]
  var i1267 = i1265[1]
  var i1266 = []
  for(var i = 0; i < i1267.length; i += 1) {
    i1266.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerLayer', i1267[i + 0]) );
  }
  i1264.layers = i1266
  var i1269 = i1265[2]
  var i1268 = []
  for(var i = 0; i < i1269.length; i += 1) {
    i1268.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerParameter', i1269[i + 0]) );
  }
  i1264.parameters = i1268
  i1264.animationClips = i1265[3]
  i1264.avatarUnsupported = i1265[4]
  return i1264
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerLayer"] = function (request, data, root) {
  var i1272 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerLayer' )
  var i1273 = data
  i1272.name = i1273[0]
  i1272.defaultWeight = i1273[1]
  i1272.blendingMode = i1273[2]
  i1272.avatarMask = i1273[3]
  i1272.syncedLayerIndex = i1273[4]
  i1272.syncedLayerAffectsTiming = !!i1273[5]
  i1272.syncedLayers = i1273[6]
  i1272.stateMachine = request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateMachine', i1273[7], i1272.stateMachine)
  return i1272
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateMachine"] = function (request, data, root) {
  var i1274 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateMachine' )
  var i1275 = data
  i1274.id = i1275[0]
  i1274.name = i1275[1]
  i1274.path = i1275[2]
  var i1277 = i1275[3]
  var i1276 = []
  for(var i = 0; i < i1277.length; i += 1) {
    i1276.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorState', i1277[i + 0]) );
  }
  i1274.states = i1276
  var i1279 = i1275[4]
  var i1278 = []
  for(var i = 0; i < i1279.length; i += 1) {
    i1278.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateMachine', i1279[i + 0]) );
  }
  i1274.machines = i1278
  var i1281 = i1275[5]
  var i1280 = []
  for(var i = 0; i < i1281.length; i += 1) {
    i1280.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorTransition', i1281[i + 0]) );
  }
  i1274.entryStateTransitions = i1280
  var i1283 = i1275[6]
  var i1282 = []
  for(var i = 0; i < i1283.length; i += 1) {
    i1282.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorTransition', i1283[i + 0]) );
  }
  i1274.exitStateTransitions = i1282
  var i1285 = i1275[7]
  var i1284 = []
  for(var i = 0; i < i1285.length; i += 1) {
    i1284.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateTransition', i1285[i + 0]) );
  }
  i1274.anyStateTransitions = i1284
  i1274.defaultStateId = i1275[8]
  return i1274
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorState"] = function (request, data, root) {
  var i1288 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorState' )
  var i1289 = data
  i1288.id = i1289[0]
  i1288.name = i1289[1]
  i1288.cycleOffset = i1289[2]
  i1288.cycleOffsetParameter = i1289[3]
  i1288.cycleOffsetParameterActive = !!i1289[4]
  i1288.mirror = !!i1289[5]
  i1288.mirrorParameter = i1289[6]
  i1288.mirrorParameterActive = !!i1289[7]
  i1288.motionId = i1289[8]
  i1288.nameHash = i1289[9]
  i1288.fullPathHash = i1289[10]
  i1288.speed = i1289[11]
  i1288.speedParameter = i1289[12]
  i1288.speedParameterActive = !!i1289[13]
  i1288.tag = i1289[14]
  i1288.tagHash = i1289[15]
  i1288.writeDefaultValues = !!i1289[16]
  var i1291 = i1289[17]
  var i1290 = []
  for(var i = 0; i < i1291.length; i += 2) {
  request.r(i1291[i + 0], i1291[i + 1], 2, i1290, '')
  }
  i1288.behaviours = i1290
  var i1293 = i1289[18]
  var i1292 = []
  for(var i = 0; i < i1293.length; i += 1) {
    i1292.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateTransition', i1293[i + 0]) );
  }
  i1288.transitions = i1292
  return i1288
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateTransition"] = function (request, data, root) {
  var i1298 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateTransition' )
  var i1299 = data
  i1298.fullPath = i1299[0]
  i1298.canTransitionToSelf = !!i1299[1]
  i1298.duration = i1299[2]
  i1298.exitTime = i1299[3]
  i1298.hasExitTime = !!i1299[4]
  i1298.hasFixedDuration = !!i1299[5]
  i1298.interruptionSource = i1299[6]
  i1298.offset = i1299[7]
  i1298.orderedInterruption = !!i1299[8]
  i1298.destinationStateId = i1299[9]
  i1298.isExit = !!i1299[10]
  i1298.mute = !!i1299[11]
  i1298.solo = !!i1299[12]
  var i1301 = i1299[13]
  var i1300 = []
  for(var i = 0; i < i1301.length; i += 1) {
    i1300.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorCondition', i1301[i + 0]) );
  }
  i1298.conditions = i1300
  return i1298
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorCondition"] = function (request, data, root) {
  var i1304 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorCondition' )
  var i1305 = data
  i1304.mode = i1305[0]
  i1304.parameter = i1305[1]
  i1304.threshold = i1305[2]
  return i1304
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorTransition"] = function (request, data, root) {
  var i1310 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorTransition' )
  var i1311 = data
  i1310.destinationStateId = i1311[0]
  i1310.isExit = !!i1311[1]
  i1310.mute = !!i1311[2]
  i1310.solo = !!i1311[3]
  var i1313 = i1311[4]
  var i1312 = []
  for(var i = 0; i < i1313.length; i += 1) {
    i1312.push( request.d('Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorCondition', i1313[i + 0]) );
  }
  i1310.conditions = i1312
  return i1310
}

Deserializers["Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerParameter"] = function (request, data, root) {
  var i1316 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerParameter' )
  var i1317 = data
  i1316.defaultBool = !!i1317[0]
  i1316.defaultFloat = i1317[1]
  i1316.defaultInt = i1317[2]
  i1316.name = i1317[3]
  i1316.nameHash = i1317[4]
  i1316.type = i1317[5]
  return i1316
}

Deserializers["CubeMovementConfigSO"] = function (request, data, root) {
  var i1318 = root || request.c( 'CubeMovementConfigSO' )
  var i1319 = data
  i1318.Speed = i1319[0]
  i1318.SlowShaderSpeed = i1319[1]
  i1318.FastShaderSpeed = i1319[2]
  i1318.Acceleration = i1319[3]
  i1318.SpawnPushSpeed = i1319[4]
  i1318.RoadGripForce = i1319[5]
  i1318.RoadGripDelay = i1319[6]
  i1318.MovementInterval = i1319[7]
  i1318.ReceiveOffset = i1319[8]
  i1318.ReceiveThreshold = i1319[9]
  i1318.SpawnFreezeYDuration = i1319[10]
  i1318.ScaleSpeedMultiplier = i1319[11]
  return i1318
}

Deserializers["CarrierMechanicVisualConfigSO"] = function (request, data, root) {
  var i1320 = root || request.c( 'CarrierMechanicVisualConfigSO' )
  var i1321 = data
  var i1323 = i1321[0]
  var i1322 = []
  for(var i = 0; i < i1323.length; i += 1) {
    i1322.push( request.d('CarrierVisualPrefabEntry', i1323[i + 0]) );
  }
  i1320.visualPrefabs = i1322
  return i1320
}

Deserializers["CarrierVisualPrefabEntry"] = function (request, data, root) {
  var i1326 = root || request.c( 'CarrierVisualPrefabEntry' )
  var i1327 = data
  i1326.Kind = i1327[0]
  request.r(i1327[1], i1327[2], 0, i1326, 'Prefab')
  return i1326
}

Deserializers["CarrierLinkedBlockVisualConfigSO"] = function (request, data, root) {
  var i1328 = root || request.c( 'CarrierLinkedBlockVisualConfigSO' )
  var i1329 = data
  var i1331 = i1329[0]
  var i1330 = new (System.Collections.Generic.List$1(Bridge.ns('LinkedBlockVisualEntry')))
  for(var i = 0; i < i1331.length; i += 1) {
    i1330.add(request.d('LinkedBlockVisualEntry', i1331[i + 0]));
  }
  i1328.linkedVisuals = i1330
  return i1328
}

Deserializers["LinkedBlockVisualEntry"] = function (request, data, root) {
  var i1334 = root || request.c( 'LinkedBlockVisualEntry' )
  var i1335 = data
  i1334.BlockCount = i1335[0]
  request.r(i1335[1], i1335[2], 0, i1334, 'Prefab')
  i1334.LocalOffset = new pc.Vec3( i1335[3], i1335[4], i1335[5] )
  return i1334
}

Deserializers["ConveyorCornerDetectorConfigSO"] = function (request, data, root) {
  var i1336 = root || request.c( 'ConveyorCornerDetectorConfigSO' )
  var i1337 = data
  i1336.CornerAngleThreshold = i1337[0]
  i1336.CornerSampleOffset = i1337[1]
  i1336.CornerScanStep = i1337[2]
  i1336.CornerProgressOffset = i1337[3]
  i1336.GizmoRadius = i1337[4]
  i1336.GizmoHeight = i1337[5]
  return i1336
}

Deserializers["CubeConfigSO"] = function (request, data, root) {
  var i1338 = root || request.c( 'CubeConfigSO' )
  var i1339 = data
  request.r(i1339[0], i1339[1], 0, i1338, 'CubePrefab')
  request.r(i1339[2], i1339[3], 0, i1338, 'AnimCubePrefab')
  i1338.CubeDefaultScale = new pc.Vec3( i1339[4], i1339[5], i1339[6] )
  return i1338
}

Deserializers["ConveyorSpawnPointConfigSO"] = function (request, data, root) {
  var i1340 = root || request.c( 'ConveyorSpawnPointConfigSO' )
  var i1341 = data
  i1340.DeliveryEdgePadding = i1341[0]
  i1340.DeliveryMinForwardSpread = i1341[1]
  i1340.DeliveryMaxForwardSpread = i1341[2]
  i1340.DeliveryLift = i1341[3]
  i1340.DeliverySpreadSideRatio = i1341[4]
  i1340.DeliveryJitterSideRatio = i1341[5]
  i1340.DeliveryForwardSpreadByLength = i1341[6]
  i1340.DeliveryJitterForwardRatio = i1341[7]
  return i1340
}

Deserializers["ConveyorSpeedBoostConfigSO"] = function (request, data, root) {
  var i1342 = root || request.c( 'ConveyorSpeedBoostConfigSO' )
  var i1343 = data
  i1342.DrawBoostRanges = !!i1343[0]
  i1342.DrawBehindLockRange = !!i1343[1]
  i1342.AheadExtraSpeed = i1343[2]
  i1342.AheadRange = i1343[3]
  i1342.AheadBoostDistance = i1343[4]
  i1342.CornerExtraSpeed = i1343[5]
  i1342.CornerBoostDistance = i1343[6]
  i1342.PortalExtraSpeed = i1343[7]
  i1342.PortalBoostDistance = i1343[8]
  i1342.PostUnloadFreezeAroundRange = i1343[9]
  i1342.PostUnloadFreezeAroundOffset = i1343[10]
  i1342.PostUnloadFreezeSpeedMultiplier = i1343[11]
  i1342.PostUnloadLockYDuration = i1343[12]
  i1342.PostUnloadSlowDuration = i1343[13]
  return i1342
}

Deserializers["CarrierConfigSO"] = function (request, data, root) {
  var i1344 = root || request.c( 'CarrierConfigSO' )
  var i1345 = data
  request.r(i1345[0], i1345[1], 0, i1344, 'Prefab')
  request.r(i1345[2], i1345[3], 0, i1344, 'ContainerMechanic')
  request.r(i1345[4], i1345[5], 0, i1344, 'Spawner')
  request.r(i1345[6], i1345[7], 0, i1344, 'BlockLinkVisualPrefab')
  i1344.Depth = i1345[8]
  i1344.BlockCount = i1345[9]
  return i1344
}

Deserializers["GameConditionConfigSO"] = function (request, data, root) {
  var i1346 = root || request.c( 'GameConditionConfigSO' )
  var i1347 = data
  i1346.WinDelaySeconds = i1347[0]
  i1346.LoseDelaySeconds = i1347[1]
  i1346.PreloseTargetSpeedMultiplier = i1347[2]
  i1346.LoseShakeDuration = i1347[3]
  i1346.LoseShakeMagnitude = i1347[4]
  i1346.EnableWinGuaranteeSpeedUp = !!i1347[5]
  i1346.WinGuaranteeSpeedMultiplier = i1347[6]
  return i1346
}

Deserializers["LevelData"] = function (request, data, root) {
  var i1348 = root || request.c( 'LevelData' )
  var i1349 = data
  i1348.LevelId = i1349[0]
  i1348.BaselineCapacity = i1349[1]
  i1348.Capacity = i1349[2]
  i1348.OrthographicSize = i1349[3]
  i1348.GoldReward = i1349[4]
  i1348.LevelType = i1349[5]
  i1348.CarrierLayout = request.d('CarrierLayoutData', i1349[6], i1348.CarrierLayout)
  i1348.SplineLayout = request.d('SplinePathData', i1349[7], i1348.SplineLayout)
  return i1348
}

Deserializers["CarrierLayoutData"] = function (request, data, root) {
  var i1350 = root || request.c( 'CarrierLayoutData' )
  var i1351 = data
  var i1353 = i1351[0]
  var i1352 = new (System.Collections.Generic.List$1(Bridge.ns('CarrierStackData')))
  for(var i = 0; i < i1353.length; i += 1) {
    i1352.add(request.d('CarrierStackData', i1353[i + 0]));
  }
  i1350.Carriers = i1352
  var i1355 = i1351[1]
  var i1354 = new (System.Collections.Generic.List$1(Bridge.ns('CarrierStackData')))
  for(var i = 0; i < i1355.length; i += 1) {
    i1354.add(request.d('CarrierStackData', i1355[i + 0]));
  }
  i1350.BoosterCarriers = i1354
  var i1357 = i1351[2]
  var i1356 = new (System.Collections.Generic.List$1(Bridge.ns('ContainerLevelData')))
  for(var i = 0; i < i1357.length; i += 1) {
    i1356.add(request.d('ContainerLevelData', i1357[i + 0]));
  }
  i1350.Containers = i1356
  return i1350
}

Deserializers["CarrierStackData"] = function (request, data, root) {
  var i1360 = root || request.c( 'CarrierStackData' )
  var i1361 = data
  i1360.Progress = i1361[0]
  i1360.Position = new pc.Vec3( i1361[1], i1361[2], i1361[3] )
  i1360.RotationY = i1361[4]
  var i1363 = i1361[5]
  var i1362 = new (System.Collections.Generic.List$1(Bridge.ns('BlockData')))
  for(var i = 0; i < i1363.length; i += 1) {
    i1362.add(request.d('BlockData', i1363[i + 0]));
  }
  i1360.Blocks = i1362
  var i1365 = i1361[6]
  var i1364 = new (System.Collections.Generic.List$1(Bridge.ns('CarrierMechanicData')))
  for(var i = 0; i < i1365.length; i += 1) {
    i1364.add(request.d('CarrierMechanicData', i1365[i + 0]));
  }
  i1360.Mechanics = i1364
  return i1360
}

Deserializers["BlockData"] = function (request, data, root) {
  var i1368 = root || request.c( 'BlockData' )
  var i1369 = data
  i1368.BlockColor = i1369[0]
  var i1371 = i1369[1]
  var i1370 = new (System.Collections.Generic.List$1(Bridge.ns('BlockMechanicData')))
  for(var i = 0; i < i1371.length; i += 1) {
    i1370.add(request.d('BlockMechanicData', i1371[i + 0]));
  }
  i1368.Mechanics = i1370
  return i1368
}

Deserializers["BlockMechanicData"] = function (request, data, root) {
  var i1374 = root || request.c( 'BlockMechanicData' )
  var i1375 = data
  i1374.Type = i1375[0]
  i1374.ContainerId = i1375[1]
  i1374.KeyColor = i1375[2]
  i1374.LinkGroupId = i1375[3]
  i1374.SwapGroupId = i1375[4]
  return i1374
}

Deserializers["CarrierMechanicData"] = function (request, data, root) {
  var i1378 = root || request.c( 'CarrierMechanicData' )
  var i1379 = data
  i1378.Type = i1379[0]
  i1378.UnlockColor = i1379[1]
  i1378.TargetColor = i1379[2]
  return i1378
}

Deserializers["ContainerLevelData"] = function (request, data, root) {
  var i1382 = root || request.c( 'ContainerLevelData' )
  var i1383 = data
  i1382.ContainerId = i1383[0]
  i1382.UnlockColor = i1383[1]
  i1382.Position = new pc.Vec3( i1383[2], i1383[3], i1383[4] )
  i1382.RotationY = i1383[5]
  i1382.ScaleXZ = i1383[6]
  var i1385 = i1383[7]
  var i1384 = new (System.Collections.Generic.List$1(Bridge.ns('System.Int32')))
  for(var i = 0; i < i1385.length; i += 1) {
    i1384.add(i1385[i + 0]);
  }
  i1382.CarrierIndexes = i1384
  return i1382
}

Deserializers["SplinePathData"] = function (request, data, root) {
  var i1386 = root || request.c( 'SplinePathData' )
  var i1387 = data
  i1386.Closed = !!i1387[0]
  var i1389 = i1387[1]
  var i1388 = new (System.Collections.Generic.List$1(Bridge.ns('SplinePointData')))
  for(var i = 0; i < i1389.length; i += 1) {
    i1388.add(request.d('SplinePointData', i1389[i + 0]));
  }
  i1386.Nodes = i1388
  return i1386
}

Deserializers["SplinePointData"] = function (request, data, root) {
  var i1392 = root || request.c( 'SplinePointData' )
  var i1393 = data
  i1392.MapPointId = i1393[0]
  i1392.GridPosition = new pc.Vec2( i1393[1], i1393[2] )
  i1392.TangentMode = i1393[3]
  i1392.TangentInValue = new pc.Vec3( i1393[4], i1393[5], i1393[6] )
  i1392.TangentOutValue = new pc.Vec3( i1393[7], i1393[8], i1393[9] )
  i1392.Rotation = new pc.Vec3( i1393[10], i1393[11], i1393[12] )
  return i1392
}

Deserializers["LevelEntryAnimConfigSO"] = function (request, data, root) {
  var i1394 = root || request.c( 'LevelEntryAnimConfigSO' )
  var i1395 = data
  i1394.ConveyorRevealDelay = i1395[0]
  i1394.ConveyorRevealDuration = i1395[1]
  i1394.ConveyorRevealEase = i1395[2]
  i1394.ContainerScaleStagger = i1395[3]
  i1394.ContainerScaleDuration = i1395[4]
  i1394.ContainerScaleEase = i1395[5]
  i1394.CarrierScaleDuration = i1395[6]
  i1394.CarrierScaleStagger = i1395[7]
  i1394.CarrierScaleEase = i1395[8]
  return i1394
}

Deserializers["ColorConfigSO"] = function (request, data, root) {
  var i1396 = root || request.c( 'ColorConfigSO' )
  var i1397 = data
  var i1399 = i1397[0]
  var i1398 = new (System.Collections.Generic.List$1(Bridge.ns('ColorEntry')))
  for(var i = 0; i < i1399.length; i += 1) {
    i1398.add(request.d('ColorEntry', i1399[i + 0]));
  }
  i1396.blockColors = i1398
  var i1401 = i1397[1]
  var i1400 = new (System.Collections.Generic.List$1(Bridge.ns('ColorMaterialSourceEntry')))
  for(var i = 0; i < i1401.length; i += 1) {
    i1400.add(request.d('ColorMaterialSourceEntry', i1401[i + 0]));
  }
  i1396.exportSources = i1400
  return i1396
}

Deserializers["ColorEntry"] = function (request, data, root) {
  var i1404 = root || request.c( 'ColorEntry' )
  var i1405 = data
  i1404.BlockColorType = i1405[0]
  i1404.Color = new pc.Color(i1405[1], i1405[2], i1405[3], i1405[4])
  i1404.ShadowColor = new pc.Color(i1405[5], i1405[6], i1405[7], i1405[8])
  i1404.SpecularColor = new pc.Color(i1405[9], i1405[10], i1405[11], i1405[12])
  i1404.RimColor = new pc.Color(i1405[13], i1405[14], i1405[15], i1405[16])
  i1404.MatCapColor = new pc.Color(i1405[17], i1405[18], i1405[19], i1405[20])
  i1404.OutlineColor = new pc.Color(i1405[21], i1405[22], i1405[23], i1405[24])
  return i1404
}

Deserializers["ColorMaterialSourceEntry"] = function (request, data, root) {
  var i1408 = root || request.c( 'ColorMaterialSourceEntry' )
  var i1409 = data
  i1408.BlockColorType = i1409[0]
  request.r(i1409[1], i1409[2], 0, i1408, 'SourceMaterial')
  return i1408
}

Deserializers["CatColorConfigSO"] = function (request, data, root) {
  var i1410 = root || request.c( 'CatColorConfigSO' )
  var i1411 = data
  var i1413 = i1411[0]
  var i1412 = new (System.Collections.Generic.List$1(Bridge.ns('CatColorEntry')))
  for(var i = 0; i < i1413.length; i += 1) {
    i1412.add(request.d('CatColorEntry', i1413[i + 0]));
  }
  i1410.CatColorEntries = i1412
  return i1410
}

Deserializers["CatColorEntry"] = function (request, data, root) {
  var i1416 = root || request.c( 'CatColorEntry' )
  var i1417 = data
  i1416.BlockColorType = i1417[0]
  i1416.Color = new pc.Color(i1417[1], i1417[2], i1417[3], i1417[4])
  return i1416
}

Deserializers["AnimBlockConfig"] = function (request, data, root) {
  var i1418 = root || request.c( 'AnimBlockConfig' )
  var i1419 = data
  var i1421 = i1419[0]
  var i1420 = new (System.Collections.Generic.List$1(Bridge.ns('DataAnim')))
  for(var i = 0; i < i1421.length; i += 1) {
    i1420.add(request.d('DataAnim', i1421[i + 0]));
  }
  i1418.DataAnims = i1420
  return i1418
}

Deserializers["DataAnim"] = function (request, data, root) {
  var i1424 = root || request.c( 'DataAnim' )
  var i1425 = data
  i1424.Type = i1425[0]
  var i1427 = i1425[1]
  var i1426 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.Vector3')))
  for(var i = 0; i < i1427.length; i += 3) {
    i1426.add(new pc.Vec3( i1427[i + 0], i1427[i + 1], i1427[i + 2] ));
  }
  i1424.LocalScales = i1426
  i1424.Duration = i1425[2]
  i1424.Ease = i1425[3]
  return i1424
}

Deserializers["StylizedColorConfigSO"] = function (request, data, root) {
  var i1430 = root || request.c( 'StylizedColorConfigSO' )
  var i1431 = data
  var i1433 = i1431[0]
  var i1432 = new (System.Collections.Generic.List$1(Bridge.ns('StylizedColorEntry')))
  for(var i = 0; i < i1433.length; i += 1) {
    i1432.add(request.d('StylizedColorEntry', i1433[i + 0]));
  }
  i1430.blockColors = i1432
  var i1435 = i1431[1]
  var i1434 = new (System.Collections.Generic.List$1(Bridge.ns('StylizedColorMaterialSourceEntry')))
  for(var i = 0; i < i1435.length; i += 1) {
    i1434.add(request.d('StylizedColorMaterialSourceEntry', i1435[i + 0]));
  }
  i1430.exportSources = i1434
  return i1430
}

Deserializers["StylizedColorEntry"] = function (request, data, root) {
  var i1438 = root || request.c( 'StylizedColorEntry' )
  var i1439 = data
  i1438.BlockColorType = i1439[0]
  i1438.Color = new pc.Color(i1439[1], i1439[2], i1439[3], i1439[4])
  i1438.ShadowColor = new pc.Color(i1439[5], i1439[6], i1439[7], i1439[8])
  i1438.SpecularColor = new pc.Color(i1439[9], i1439[10], i1439[11], i1439[12])
  i1438.ReflectColor = new pc.Color(i1439[13], i1439[14], i1439[15], i1439[16])
  return i1438
}

Deserializers["StylizedColorMaterialSourceEntry"] = function (request, data, root) {
  var i1442 = root || request.c( 'StylizedColorMaterialSourceEntry' )
  var i1443 = data
  i1442.BlockColorType = i1443[0]
  request.r(i1443[1], i1443[2], 0, i1442, 'SourceMaterial')
  return i1442
}

Deserializers["RemainingColorConfigSO"] = function (request, data, root) {
  var i1444 = root || request.c( 'RemainingColorConfigSO' )
  var i1445 = data
  i1444.noneColor = new pc.Color(i1445[0], i1445[1], i1445[2], i1445[3])
  var i1447 = i1445[4]
  var i1446 = new (System.Collections.Generic.List$1(Bridge.ns('RemainingColorEntry')))
  for(var i = 0; i < i1447.length; i += 1) {
    i1446.add(request.d('RemainingColorEntry', i1447[i + 0]));
  }
  i1444.remainingColorEntries = i1446
  return i1444
}

Deserializers["RemainingColorEntry"] = function (request, data, root) {
  var i1450 = root || request.c( 'RemainingColorEntry' )
  var i1451 = data
  i1450.BlockColorType = i1451[0]
  i1450.Color = new pc.Color(i1451[1], i1451[2], i1451[3], i1451[4])
  return i1450
}

Deserializers["DG.Tweening.Core.DOTweenSettings"] = function (request, data, root) {
  var i1452 = root || request.c( 'DG.Tweening.Core.DOTweenSettings' )
  var i1453 = data
  i1452.useSafeMode = !!i1453[0]
  i1452.safeModeOptions = request.d('DG.Tweening.Core.DOTweenSettings+SafeModeOptions', i1453[1], i1452.safeModeOptions)
  i1452.timeScale = i1453[2]
  i1452.unscaledTimeScale = i1453[3]
  i1452.useSmoothDeltaTime = !!i1453[4]
  i1452.maxSmoothUnscaledTime = i1453[5]
  i1452.rewindCallbackMode = i1453[6]
  i1452.showUnityEditorReport = !!i1453[7]
  i1452.logBehaviour = i1453[8]
  i1452.drawGizmos = !!i1453[9]
  i1452.defaultRecyclable = !!i1453[10]
  i1452.defaultAutoPlay = i1453[11]
  i1452.defaultUpdateType = i1453[12]
  i1452.defaultTimeScaleIndependent = !!i1453[13]
  i1452.defaultEaseType = i1453[14]
  i1452.defaultEaseOvershootOrAmplitude = i1453[15]
  i1452.defaultEasePeriod = i1453[16]
  i1452.defaultAutoKill = !!i1453[17]
  i1452.defaultLoopType = i1453[18]
  i1452.debugMode = !!i1453[19]
  i1452.debugStoreTargetId = !!i1453[20]
  i1452.showPreviewPanel = !!i1453[21]
  i1452.storeSettingsLocation = i1453[22]
  i1452.modules = request.d('DG.Tweening.Core.DOTweenSettings+ModulesSetup', i1453[23], i1452.modules)
  i1452.createASMDEF = !!i1453[24]
  i1452.showPlayingTweens = !!i1453[25]
  i1452.showPausedTweens = !!i1453[26]
  return i1452
}

Deserializers["DG.Tweening.Core.DOTweenSettings+SafeModeOptions"] = function (request, data, root) {
  var i1454 = root || request.c( 'DG.Tweening.Core.DOTweenSettings+SafeModeOptions' )
  var i1455 = data
  i1454.logBehaviour = i1455[0]
  i1454.nestedTweenFailureBehaviour = i1455[1]
  return i1454
}

Deserializers["DG.Tweening.Core.DOTweenSettings+ModulesSetup"] = function (request, data, root) {
  var i1456 = root || request.c( 'DG.Tweening.Core.DOTweenSettings+ModulesSetup' )
  var i1457 = data
  i1456.showPanel = !!i1457[0]
  i1456.audioEnabled = !!i1457[1]
  i1456.physicsEnabled = !!i1457[2]
  i1456.physics2DEnabled = !!i1457[3]
  i1456.spriteEnabled = !!i1457[4]
  i1456.uiEnabled = !!i1457[5]
  i1456.textMeshProEnabled = !!i1457[6]
  i1456.tk2DEnabled = !!i1457[7]
  i1456.deAudioEnabled = !!i1457[8]
  i1456.deUnityExtendedEnabled = !!i1457[9]
  i1456.epoOutlineEnabled = !!i1457[10]
  return i1456
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Resources"] = function (request, data, root) {
  var i1458 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Resources' )
  var i1459 = data
  var i1461 = i1459[0]
  var i1460 = []
  for(var i = 0; i < i1461.length; i += 1) {
    i1460.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Resources+File', i1461[i + 0]) );
  }
  i1458.files = i1460
  i1458.componentToPrefabIds = i1459[1]
  return i1458
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Resources+File"] = function (request, data, root) {
  var i1464 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Resources+File' )
  var i1465 = data
  i1464.path = i1465[0]
  request.r(i1465[1], i1465[2], 0, i1464, 'unityObject')
  return i1464
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings"] = function (request, data, root) {
  var i1466 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings' )
  var i1467 = data
  var i1469 = i1467[0]
  var i1468 = []
  for(var i = 0; i < i1469.length; i += 1) {
    i1468.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder', i1469[i + 0]) );
  }
  i1466.scriptsExecutionOrder = i1468
  var i1471 = i1467[1]
  var i1470 = []
  for(var i = 0; i < i1471.length; i += 1) {
    i1470.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer', i1471[i + 0]) );
  }
  i1466.sortingLayers = i1470
  var i1473 = i1467[2]
  var i1472 = []
  for(var i = 0; i < i1473.length; i += 1) {
    i1472.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer', i1473[i + 0]) );
  }
  i1466.cullingLayers = i1472
  i1466.timeSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings', i1467[3], i1466.timeSettings)
  i1466.physicsSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings', i1467[4], i1466.physicsSettings)
  i1466.physics2DSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings', i1467[5], i1466.physics2DSettings)
  i1466.qualitySettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.QualitySettings', i1467[6], i1466.qualitySettings)
  i1466.enableRealtimeShadows = !!i1467[7]
  i1466.enableAutoInstancing = !!i1467[8]
  i1466.enableStaticBatching = !!i1467[9]
  i1466.enableDynamicBatching = !!i1467[10]
  i1466.usePreservativeDynamicBatching = !!i1467[11]
  i1466.lightmapEncodingQuality = i1467[12]
  i1466.desiredColorSpace = i1467[13]
  var i1475 = i1467[14]
  var i1474 = []
  for(var i = 0; i < i1475.length; i += 1) {
    i1474.push( i1475[i + 0] );
  }
  i1466.allTags = i1474
  return i1466
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder"] = function (request, data, root) {
  var i1478 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder' )
  var i1479 = data
  i1478.name = i1479[0]
  i1478.value = i1479[1]
  return i1478
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer"] = function (request, data, root) {
  var i1482 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer' )
  var i1483 = data
  i1482.id = i1483[0]
  i1482.name = i1483[1]
  i1482.value = i1483[2]
  return i1482
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer"] = function (request, data, root) {
  var i1486 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer' )
  var i1487 = data
  i1486.id = i1487[0]
  i1486.name = i1487[1]
  return i1486
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings"] = function (request, data, root) {
  var i1488 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings' )
  var i1489 = data
  i1488.fixedDeltaTime = i1489[0]
  i1488.maximumDeltaTime = i1489[1]
  i1488.timeScale = i1489[2]
  i1488.maximumParticleTimestep = i1489[3]
  return i1488
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings"] = function (request, data, root) {
  var i1490 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings' )
  var i1491 = data
  i1490.gravity = new pc.Vec3( i1491[0], i1491[1], i1491[2] )
  i1490.defaultSolverIterations = i1491[3]
  i1490.bounceThreshold = i1491[4]
  i1490.autoSyncTransforms = !!i1491[5]
  i1490.autoSimulation = !!i1491[6]
  var i1493 = i1491[7]
  var i1492 = []
  for(var i = 0; i < i1493.length; i += 1) {
    i1492.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask', i1493[i + 0]) );
  }
  i1490.collisionMatrix = i1492
  return i1490
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask"] = function (request, data, root) {
  var i1496 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask' )
  var i1497 = data
  i1496.enabled = !!i1497[0]
  i1496.layerId = i1497[1]
  i1496.otherLayerId = i1497[2]
  return i1496
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings"] = function (request, data, root) {
  var i1498 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings' )
  var i1499 = data
  request.r(i1499[0], i1499[1], 0, i1498, 'material')
  i1498.gravity = new pc.Vec2( i1499[2], i1499[3] )
  i1498.positionIterations = i1499[4]
  i1498.velocityIterations = i1499[5]
  i1498.velocityThreshold = i1499[6]
  i1498.maxLinearCorrection = i1499[7]
  i1498.maxAngularCorrection = i1499[8]
  i1498.maxTranslationSpeed = i1499[9]
  i1498.maxRotationSpeed = i1499[10]
  i1498.baumgarteScale = i1499[11]
  i1498.baumgarteTOIScale = i1499[12]
  i1498.timeToSleep = i1499[13]
  i1498.linearSleepTolerance = i1499[14]
  i1498.angularSleepTolerance = i1499[15]
  i1498.defaultContactOffset = i1499[16]
  i1498.autoSimulation = !!i1499[17]
  i1498.queriesHitTriggers = !!i1499[18]
  i1498.queriesStartInColliders = !!i1499[19]
  i1498.callbacksOnDisable = !!i1499[20]
  i1498.reuseCollisionCallbacks = !!i1499[21]
  i1498.autoSyncTransforms = !!i1499[22]
  var i1501 = i1499[23]
  var i1500 = []
  for(var i = 0; i < i1501.length; i += 1) {
    i1500.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask', i1501[i + 0]) );
  }
  i1498.collisionMatrix = i1500
  return i1498
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask"] = function (request, data, root) {
  var i1504 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask' )
  var i1505 = data
  i1504.enabled = !!i1505[0]
  i1504.layerId = i1505[1]
  i1504.otherLayerId = i1505[2]
  return i1504
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.QualitySettings"] = function (request, data, root) {
  var i1506 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.QualitySettings' )
  var i1507 = data
  var i1509 = i1507[0]
  var i1508 = []
  for(var i = 0; i < i1509.length; i += 1) {
    i1508.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.QualitySettings', i1509[i + 0]) );
  }
  i1506.qualityLevels = i1508
  var i1511 = i1507[1]
  var i1510 = []
  for(var i = 0; i < i1511.length; i += 1) {
    i1510.push( i1511[i + 0] );
  }
  i1506.names = i1510
  i1506.shadows = i1507[2]
  i1506.anisotropicFiltering = i1507[3]
  i1506.antiAliasing = i1507[4]
  i1506.lodBias = i1507[5]
  i1506.shadowCascades = i1507[6]
  i1506.shadowDistance = i1507[7]
  i1506.shadowmaskMode = i1507[8]
  i1506.shadowProjection = i1507[9]
  i1506.shadowResolution = i1507[10]
  i1506.softParticles = !!i1507[11]
  i1506.softVegetation = !!i1507[12]
  i1506.activeColorSpace = i1507[13]
  i1506.desiredColorSpace = i1507[14]
  i1506.masterTextureLimit = i1507[15]
  i1506.maxQueuedFrames = i1507[16]
  i1506.particleRaycastBudget = i1507[17]
  i1506.pixelLightCount = i1507[18]
  i1506.realtimeReflectionProbes = !!i1507[19]
  i1506.shadowCascade2Split = i1507[20]
  i1506.shadowCascade4Split = new pc.Vec3( i1507[21], i1507[22], i1507[23] )
  i1506.streamingMipmapsActive = !!i1507[24]
  i1506.vSyncCount = i1507[25]
  i1506.asyncUploadBufferSize = i1507[26]
  i1506.asyncUploadTimeSlice = i1507[27]
  i1506.billboardsFaceCameraPosition = !!i1507[28]
  i1506.shadowNearPlaneOffset = i1507[29]
  i1506.streamingMipmapsMemoryBudget = i1507[30]
  i1506.maximumLODLevel = i1507[31]
  i1506.streamingMipmapsAddAllCameras = !!i1507[32]
  i1506.streamingMipmapsMaxLevelReduction = i1507[33]
  i1506.streamingMipmapsRenderersPerFrame = i1507[34]
  i1506.resolutionScalingFixedDPIFactor = i1507[35]
  i1506.streamingMipmapsMaxFileIORequests = i1507[36]
  i1506.currentQualityLevel = i1507[37]
  return i1506
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame"] = function (request, data, root) {
  var i1516 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame' )
  var i1517 = data
  i1516.weight = i1517[0]
  i1516.vertices = i1517[1]
  i1516.normals = i1517[2]
  i1516.tangents = i1517[3]
  return i1516
}

Deserializers["UnityEngine.Splines.SplineKnotIndex"] = function (request, data, root) {
  var i1520 = root || request.c( 'UnityEngine.Splines.SplineKnotIndex' )
  var i1521 = data
  i1520.Spline = i1521[0]
  i1520.Knot = i1521[1]
  return i1520
}

Deserializers.fields = {"Luna.Unity.DTO.UnityEngine.Assets.Material":{"name":0,"shader":1,"renderQueue":3,"enableInstancing":4,"floatParameters":5,"colorParameters":6,"vectorParameters":7,"textureParameters":8,"materialFlags":9},"Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag":{"name":0,"enabled":1},"Luna.Unity.DTO.UnityEngine.Textures.Texture2D":{"name":0,"width":1,"height":2,"mipmapCount":3,"anisoLevel":4,"filterMode":5,"hdr":6,"format":7,"wrapMode":8,"alphaIsTransparency":9,"alphaSource":10,"graphicsFormat":11,"sRGBTexture":12,"desiredColorSpace":13,"wrapU":14,"wrapV":15},"Luna.Unity.DTO.UnityEngine.Assets.Mesh":{"name":0,"halfPrecision":1,"useSimplification":2,"useUInt32IndexFormat":3,"vertexCount":4,"aabb":5,"streams":6,"vertices":7,"subMeshes":8,"bindposes":9,"blendShapes":10},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh":{"triangles":0},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape":{"name":0,"frames":1},"Luna.Unity.DTO.UnityEngine.Textures.Cubemap":{"name":0,"atlasId":1,"mipmapCount":2,"hdr":3,"size":4,"anisoLevel":5,"filterMode":6,"rects":7,"wrapU":8,"wrapV":9},"Luna.Unity.DTO.UnityEngine.Components.Transform":{"position":0,"scale":3,"rotation":6},"Luna.Unity.DTO.UnityEngine.Components.MeshFilter":{"sharedMesh":0},"Luna.Unity.DTO.UnityEngine.Components.MeshRenderer":{"additionalVertexStreams":0,"enabled":2,"sharedMaterial":3,"sharedMaterials":5,"receiveShadows":6,"shadowCastingMode":7,"sortingLayerID":8,"sortingOrder":9,"lightmapIndex":10,"lightmapSceneIndex":11,"lightmapScaleOffset":12,"lightProbeUsage":16,"reflectionProbeUsage":17},"Luna.Unity.DTO.UnityEngine.Scene.GameObject":{"name":0,"tagId":1,"enabled":2,"isStatic":3,"layer":4},"Luna.Unity.DTO.UnityEngine.Components.BoxCollider":{"center":0,"size":3,"enabled":6,"isTrigger":7,"material":8},"Luna.Unity.DTO.UnityEngine.Components.Animator":{"animatorController":0,"avatar":2,"updateMode":4,"hasTransformHierarchy":5,"applyRootMotion":6,"humanBones":7,"enabled":8},"Luna.Unity.DTO.UnityEngine.Components.SpriteRenderer":{"color":0,"sprite":4,"flipX":6,"flipY":7,"drawMode":8,"size":9,"tileMode":11,"adaptiveModeThreshold":12,"maskInteraction":13,"spriteSortPoint":14,"enabled":15,"sharedMaterial":16,"sharedMaterials":18,"receiveShadows":19,"shadowCastingMode":20,"sortingLayerID":21,"sortingOrder":22,"lightmapIndex":23,"lightmapSceneIndex":24,"lightmapScaleOffset":25,"lightProbeUsage":29,"reflectionProbeUsage":30},"Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer":{"sharedMesh":0,"bones":2,"updateWhenOffscreen":3,"localBounds":4,"rootBone":5,"blendShapesWeights":7,"enabled":8,"sharedMaterial":9,"sharedMaterials":11,"receiveShadows":12,"shadowCastingMode":13,"sortingLayerID":14,"sortingOrder":15,"lightmapIndex":16,"lightmapSceneIndex":17,"lightmapScaleOffset":18,"lightProbeUsage":22,"reflectionProbeUsage":23},"Luna.Unity.DTO.UnityEngine.Components.SkinnedMeshRenderer+BlendShapeWeight":{"weight":0},"Luna.Unity.DTO.UnityEngine.Components.ParticleSystem":{"main":0,"colorBySpeed":1,"colorOverLifetime":2,"emission":3,"rotationBySpeed":4,"rotationOverLifetime":5,"shape":6,"sizeBySpeed":7,"sizeOverLifetime":8,"textureSheetAnimation":9,"velocityOverLifetime":10,"noise":11,"inheritVelocity":12,"forceOverLifetime":13,"limitVelocityOverLifetime":14,"useAutoRandomSeed":15,"randomSeed":16},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.MainModule":{"duration":0,"loop":1,"prewarm":2,"startDelay":3,"startLifetime":4,"startSpeed":5,"startSize3D":6,"startSizeX":7,"startSizeY":8,"startSizeZ":9,"startRotation3D":10,"startRotationX":11,"startRotationY":12,"startRotationZ":13,"startColor":14,"gravityModifier":15,"simulationSpace":16,"customSimulationSpace":17,"simulationSpeed":19,"useUnscaledTime":20,"scalingMode":21,"playOnAwake":22,"maxParticles":23,"emitterVelocityMode":24,"stopAction":25},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxCurve":{"mode":0,"curveMin":1,"curveMax":2,"curveMultiplier":3,"constantMin":4,"constantMax":5},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.MinMaxGradient":{"mode":0,"gradientMin":1,"gradientMax":2,"colorMin":3,"colorMax":7},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Gradient":{"mode":0,"colorKeys":1,"alphaKeys":2},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorBySpeedModule":{"enabled":0,"color":1,"range":2},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientColorKey":{"color":0,"time":4},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Data.GradientAlphaKey":{"alpha":0,"time":1},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ColorOverLifetimeModule":{"enabled":0,"color":1},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.EmissionModule":{"enabled":0,"rateOverTime":1,"rateOverDistance":2,"bursts":3},"Luna.Unity.DTO.UnityEngine.ParticleSystemTypes.Burst":{"count":0,"cycleCount":1,"minCount":2,"maxCount":3,"repeatInterval":4,"time":5},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationBySpeedModule":{"enabled":0,"x":1,"y":2,"z":3,"separateAxes":4,"range":5},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.RotationOverLifetimeModule":{"enabled":0,"x":1,"y":2,"z":3,"separateAxes":4},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ShapeModule":{"enabled":0,"shapeType":1,"randomDirectionAmount":2,"sphericalDirectionAmount":3,"randomPositionAmount":4,"alignToDirection":5,"radius":6,"radiusMode":7,"radiusSpread":8,"radiusSpeed":9,"radiusThickness":10,"angle":11,"length":12,"boxThickness":13,"meshShapeType":16,"mesh":17,"meshRenderer":19,"skinnedMeshRenderer":21,"useMeshMaterialIndex":23,"meshMaterialIndex":24,"useMeshColors":25,"normalOffset":26,"arc":27,"arcMode":28,"arcSpread":29,"arcSpeed":30,"donutRadius":31,"position":32,"rotation":35,"scale":38},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeBySpeedModule":{"enabled":0,"x":1,"y":2,"z":3,"separateAxes":4,"range":5},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.SizeOverLifetimeModule":{"enabled":0,"x":1,"y":2,"z":3,"separateAxes":4},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.TextureSheetAnimationModule":{"enabled":0,"mode":1,"animation":2,"numTilesX":3,"numTilesY":4,"useRandomRow":5,"frameOverTime":6,"startFrame":7,"cycleCount":8,"rowIndex":9,"flipU":10,"flipV":11,"spriteCount":12,"sprites":13},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.VelocityOverLifetimeModule":{"enabled":0,"x":1,"y":2,"z":3,"radial":4,"speedModifier":5,"space":6,"orbitalX":7,"orbitalY":8,"orbitalZ":9,"orbitalOffsetX":10,"orbitalOffsetY":11,"orbitalOffsetZ":12},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.NoiseModule":{"enabled":0,"separateAxes":1,"strengthX":2,"strengthY":3,"strengthZ":4,"frequency":5,"damping":6,"octaveCount":7,"octaveMultiplier":8,"octaveScale":9,"quality":10,"scrollSpeed":11,"scrollSpeedMultiplier":12,"remapEnabled":13,"remapX":14,"remapY":15,"remapZ":16,"positionAmount":17,"rotationAmount":18,"sizeAmount":19},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.InheritVelocityModule":{"enabled":0,"mode":1,"curve":2},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.ForceOverLifetimeModule":{"enabled":0,"x":1,"y":2,"z":3,"space":4,"randomized":5},"Luna.Unity.DTO.UnityEngine.ParticleSystemModules.LimitVelocityOverLifetimeModule":{"enabled":0,"limit":1,"limitX":2,"limitY":3,"limitZ":4,"dampen":5,"separateAxes":6,"space":7,"drag":8,"multiplyDragByParticleSize":9,"multiplyDragByParticleVelocity":10},"Luna.Unity.DTO.UnityEngine.Components.ParticleSystemRenderer":{"mesh":0,"meshCount":2,"activeVertexStreamsCount":3,"alignment":4,"renderMode":5,"sortMode":6,"lengthScale":7,"velocityScale":8,"cameraVelocityScale":9,"normalDirection":10,"sortingFudge":11,"minParticleSize":12,"maxParticleSize":13,"pivot":14,"trailMaterial":17,"applyActiveColorSpace":19,"enabled":20,"sharedMaterial":21,"sharedMaterials":23,"receiveShadows":24,"shadowCastingMode":25,"sortingLayerID":26,"sortingOrder":27,"lightmapIndex":28,"lightmapSceneIndex":29,"lightmapScaleOffset":30,"lightProbeUsage":34,"reflectionProbeUsage":35},"Luna.Unity.DTO.UnityEngine.Components.SphereCollider":{"center":0,"radius":3,"enabled":4,"isTrigger":5,"material":6},"Luna.Unity.DTO.UnityEngine.Components.Rigidbody":{"mass":0,"drag":1,"angularDrag":2,"useGravity":3,"isKinematic":4,"constraints":5,"maxAngularVelocity":6,"collisionDetectionMode":7,"interpolation":8},"Luna.Unity.DTO.UnityEngine.Components.MeshCollider":{"sharedMesh":0,"convex":2,"enabled":3,"isTrigger":4,"material":5},"Luna.Unity.DTO.UnityEngine.Components.RectTransform":{"pivot":0,"anchorMin":2,"anchorMax":4,"sizeDelta":6,"anchoredPosition3D":8,"rotation":11,"scale":15},"Luna.Unity.DTO.UnityEngine.Scene.Scene":{"name":0,"index":1,"startup":2},"Luna.Unity.DTO.UnityEngine.Components.Light":{"type":0,"color":1,"cullingMask":5,"intensity":6,"range":7,"spotAngle":8,"shadows":9,"shadowNormalBias":10,"shadowBias":11,"shadowStrength":12,"shadowResolution":13,"lightmapBakeType":14,"renderMode":15,"cookie":16,"cookieSize":18,"shadowNearPlane":19,"occlusionMaskChannel":20,"isBaked":21,"mixedLightingMode":22,"enabled":23},"Luna.Unity.DTO.UnityEngine.Components.Camera":{"aspect":0,"orthographic":1,"orthographicSize":2,"backgroundColor":3,"nearClipPlane":7,"farClipPlane":8,"fieldOfView":9,"depth":10,"clearFlags":11,"cullingMask":12,"rect":13,"targetTexture":14,"usePhysicalProperties":16,"focalLength":17,"sensorSize":18,"lensShift":20,"gateFit":22,"commandBufferCount":23,"cameraType":24,"enabled":25},"Luna.Unity.DTO.UnityEngine.Components.Canvas":{"planeDistance":0,"referencePixelsPerUnit":1,"isFallbackOverlay":2,"renderMode":3,"renderOrder":4,"sortingLayerName":5,"sortingOrder":6,"scaleFactor":7,"worldCamera":8,"overrideSorting":10,"pixelPerfect":11,"targetDisplay":12,"overridePixelPerfect":13,"enabled":14},"Luna.Unity.DTO.UnityEngine.Components.CanvasRenderer":{"cullTransparentMesh":0},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings":{"ambientIntensity":0,"reflectionIntensity":1,"ambientMode":2,"ambientLight":3,"ambientSkyColor":7,"ambientGroundColor":11,"ambientEquatorColor":15,"fogColor":19,"fogEndDistance":23,"fogStartDistance":24,"fogDensity":25,"fog":26,"skybox":27,"fogMode":29,"lightmaps":30,"lightProbes":31,"lightmapsMode":32,"mixedBakeMode":33,"environmentLightingMode":34,"ambientProbe":35,"customReflection":36,"defaultReflection":38,"defaultReflectionMode":40,"defaultReflectionResolution":41,"sunLightObjectId":42,"pixelLightCount":43,"defaultReflectionHDR":44,"hasLightDataAsset":45,"hasManualGenerate":46},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap":{"lightmapColor":0,"lightmapDirection":2,"shadowMask":4},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes":{"bakedProbes":0,"positions":1,"hullRays":2,"tetrahedra":3,"neighbours":4,"matrices":5},"Luna.Unity.DTO.UnityEngine.Assets.PhysicMaterial":{"name":0,"bounciness":1,"dynamicFriction":2,"staticFriction":3,"frictionCombine":4,"bounceCombine":5},"Luna.Unity.DTO.UnityEngine.Assets.Shader":{"ShaderCompilationErrors":0,"name":1,"guid":2,"shaderDefinedKeywords":3,"passes":4,"usePasses":5,"defaultParameterValues":6,"unityFallbackShader":7,"readDepth":9,"hasDepthOnlyPass":10,"isCreatedByShaderGraph":11,"disableBatching":12,"compiled":13},"Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError":{"shaderName":0,"errorMessage":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass":{"id":0,"subShaderIndex":1,"name":2,"passType":3,"grabPassTextureName":4,"usePass":5,"zTest":6,"zWrite":7,"culling":8,"blending":9,"alphaBlending":10,"colorWriteMask":11,"offsetUnits":12,"offsetFactor":13,"stencilRef":14,"stencilReadMask":15,"stencilWriteMask":16,"stencilOp":17,"stencilOpFront":18,"stencilOpBack":19,"tags":20,"passDefinedKeywords":21,"passDefinedKeywordGroups":22,"variants":23,"excludedVariants":24,"hasDepthReader":25},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value":{"val":0,"name":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending":{"src":0,"dst":1,"op":2},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp":{"pass":0,"fail":1,"zFail":2,"comp":3},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup":{"keywords":0,"hasDiscard":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant":{"passId":0,"subShaderIndex":1,"keywords":2,"vertexProgram":3,"fragmentProgram":4,"exportedForWebGl2":5,"readDepth":6},"Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass":{"shader":0,"pass":2},"Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue":{"name":0,"type":1,"value":2,"textureValue":6,"shaderPropertyFlag":7},"Luna.Unity.DTO.UnityEngine.Textures.Sprite":{"name":0,"texture":1,"aabb":3,"vertices":4,"triangles":5,"textureRect":6,"packedRect":10,"border":14,"transparency":18,"bounds":19,"pixelsPerUnit":20,"textureWidth":21,"textureHeight":22,"nativeSize":23,"pivot":25,"textureRectOffset":27},"Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip":{"name":0,"wrapMode":1,"isLooping":2,"length":3,"curves":4,"events":5,"halfPrecision":6,"_frameRate":7,"localBounds":8,"hasMuscleCurves":9,"clipMuscleConstant":10,"clipBindingConstant":11},"Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve":{"path":0,"hash":1,"componentType":2,"property":3,"keys":4,"objectReferenceKeys":5},"Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationCurve+ObjectReferenceKey":{"time":0,"value":1},"Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationEvent":{"functionName":0,"floatParameter":1,"intParameter":2,"stringParameter":3,"objectReferenceParameter":4,"time":6},"Luna.Unity.DTO.UnityEngine.Animation.Data.Bounds":{"center":0,"extends":3},"Luna.Unity.DTO.UnityEngine.Animation.Data.AnimationClip+AnimationClipBindingConstant":{"genericBindings":0,"pptrCurveMapping":1},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorController":{"name":0,"layers":1,"parameters":2,"animationClips":3,"avatarUnsupported":4},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerLayer":{"name":0,"defaultWeight":1,"blendingMode":2,"avatarMask":3,"syncedLayerIndex":4,"syncedLayerAffectsTiming":5,"syncedLayers":6,"stateMachine":7},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateMachine":{"id":0,"name":1,"path":2,"states":3,"machines":4,"entryStateTransitions":5,"exitStateTransitions":6,"anyStateTransitions":7,"defaultStateId":8},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorState":{"id":0,"name":1,"cycleOffset":2,"cycleOffsetParameter":3,"cycleOffsetParameterActive":4,"mirror":5,"mirrorParameter":6,"mirrorParameterActive":7,"motionId":8,"nameHash":9,"fullPathHash":10,"speed":11,"speedParameter":12,"speedParameterActive":13,"tag":14,"tagHash":15,"writeDefaultValues":16,"behaviours":17,"transitions":18},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorStateTransition":{"fullPath":0,"canTransitionToSelf":1,"duration":2,"exitTime":3,"hasExitTime":4,"hasFixedDuration":5,"interruptionSource":6,"offset":7,"orderedInterruption":8,"destinationStateId":9,"isExit":10,"mute":11,"solo":12,"conditions":13},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorCondition":{"mode":0,"parameter":1,"threshold":2},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorTransition":{"destinationStateId":0,"isExit":1,"mute":2,"solo":3,"conditions":4},"Luna.Unity.DTO.UnityEngine.Animation.Mecanim.AnimatorControllerParameter":{"defaultBool":0,"defaultFloat":1,"defaultInt":2,"name":3,"nameHash":4,"type":5},"Luna.Unity.DTO.UnityEngine.Assets.Resources":{"files":0,"componentToPrefabIds":1},"Luna.Unity.DTO.UnityEngine.Assets.Resources+File":{"path":0,"unityObject":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings":{"scriptsExecutionOrder":0,"sortingLayers":1,"cullingLayers":2,"timeSettings":3,"physicsSettings":4,"physics2DSettings":5,"qualitySettings":6,"enableRealtimeShadows":7,"enableAutoInstancing":8,"enableStaticBatching":9,"enableDynamicBatching":10,"usePreservativeDynamicBatching":11,"lightmapEncodingQuality":12,"desiredColorSpace":13,"allTags":14},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer":{"id":0,"name":1,"value":2},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer":{"id":0,"name":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings":{"fixedDeltaTime":0,"maximumDeltaTime":1,"timeScale":2,"maximumParticleTimestep":3},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings":{"gravity":0,"defaultSolverIterations":3,"bounceThreshold":4,"autoSyncTransforms":5,"autoSimulation":6,"collisionMatrix":7},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask":{"enabled":0,"layerId":1,"otherLayerId":2},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings":{"material":0,"gravity":2,"positionIterations":4,"velocityIterations":5,"velocityThreshold":6,"maxLinearCorrection":7,"maxAngularCorrection":8,"maxTranslationSpeed":9,"maxRotationSpeed":10,"baumgarteScale":11,"baumgarteTOIScale":12,"timeToSleep":13,"linearSleepTolerance":14,"angularSleepTolerance":15,"defaultContactOffset":16,"autoSimulation":17,"queriesHitTriggers":18,"queriesStartInColliders":19,"callbacksOnDisable":20,"reuseCollisionCallbacks":21,"autoSyncTransforms":22,"collisionMatrix":23},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask":{"enabled":0,"layerId":1,"otherLayerId":2},"Luna.Unity.DTO.UnityEngine.Assets.QualitySettings":{"qualityLevels":0,"names":1,"shadows":2,"anisotropicFiltering":3,"antiAliasing":4,"lodBias":5,"shadowCascades":6,"shadowDistance":7,"shadowmaskMode":8,"shadowProjection":9,"shadowResolution":10,"softParticles":11,"softVegetation":12,"activeColorSpace":13,"desiredColorSpace":14,"masterTextureLimit":15,"maxQueuedFrames":16,"particleRaycastBudget":17,"pixelLightCount":18,"realtimeReflectionProbes":19,"shadowCascade2Split":20,"shadowCascade4Split":21,"streamingMipmapsActive":24,"vSyncCount":25,"asyncUploadBufferSize":26,"asyncUploadTimeSlice":27,"billboardsFaceCameraPosition":28,"shadowNearPlaneOffset":29,"streamingMipmapsMemoryBudget":30,"maximumLODLevel":31,"streamingMipmapsAddAllCameras":32,"streamingMipmapsMaxLevelReduction":33,"streamingMipmapsRenderersPerFrame":34,"resolutionScalingFixedDPIFactor":35,"streamingMipmapsMaxFileIORequests":36,"currentQualityLevel":37},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame":{"weight":0,"vertices":1,"normals":2,"tangents":3}}

Deserializers.requiredComponents = {"88":[89],"90":[89],"91":[89],"92":[89],"93":[89],"94":[89],"95":[17],"96":[52],"97":[30],"98":[30],"99":[30],"100":[30],"101":[30],"102":[30],"103":[30],"104":[105],"106":[105],"107":[105],"108":[105],"109":[105],"110":[105],"111":[105],"112":[105],"113":[105],"114":[105],"115":[105],"116":[105],"117":[105],"118":[52],"119":[6],"120":[121],"122":[121],"53":[47],"123":[7,6],"124":[47],"125":[6,47],"126":[47,56],"127":[47],"128":[56,47],"129":[6],"130":[56,47],"131":[47],"132":[47],"133":[47],"134":[53],"57":[56,47],"135":[47],"55":[53],"136":[47],"137":[47],"138":[47],"139":[47],"140":[47],"141":[47],"142":[47],"143":[47],"144":[47],"145":[56,47],"146":[47],"147":[47],"148":[47],"149":[47],"150":[56,47],"151":[47],"152":[153],"154":[153],"155":[153],"156":[153],"157":[52],"158":[52]}

Deserializers.types = ["UnityEngine.Shader","UnityEngine.Texture2D","UnityEngine.Cubemap","UnityEngine.Transform","UnityEngine.MonoBehaviour","HiddenCarrierVisual","UnityEngine.MeshRenderer","UnityEngine.MeshFilter","UnityEngine.Mesh","UnityEngine.Material","SpecialColorReceiverVisual","LinkedBlockVisual","UnityEngine.GameObject","BlockSolidProgressAnimator","UnityEngine.BoxCollider","UnityEngine.Animator","UnityEditor.Animations.AnimatorController","UnityEngine.SkinnedMeshRenderer","UnityEngine.SpriteRenderer","UnityEngine.Sprite","Block","BlockVisual","ContainerKey","BlockSolidVisual","UnityEngine.ParticleSystem","KeyAnim","UnityEngine.ParticleSystemRenderer","ConveyorPortal","UnityEngine.SphereCollider","UnityEngine.PhysicMaterial","UnityEngine.Rigidbody","Cube","CubeMovement","CubeDeliveryHandler","CubeVisual","CubeMovementConfigSO","AnimCube","Carrier","CarrierBlockLayout","CarrierMechanicVisualConfigSO","CarrierLinkedBlockVisualConfigSO","CarrierSpawnEffect","ContainerMechanic","GiftBoxVisual","Key3DCodeAnimator","UnityEngine.MeshCollider","Spawner","UnityEngine.RectTransform","SpawnerRemainingSlimeAnimator","SpawnerBlockAnimation","BlockLinkVisual","UnityEngine.Light","UnityEngine.Camera","UnityEngine.Canvas","UnityEngine.EventSystems.UIBehaviour","UnityEngine.UI.CanvasScaler","UnityEngine.CanvasRenderer","UnityEngine.UI.Image","UnityEngine.Splines.SplineContainer","ConveyorManager","UnityEngine.Splines.SplineInstantiate","ConveyorMeshBuilder","ConveyorCornerDetector","ConveyorCornerDetectorConfigSO","ConveyorDeliverySystem","CubeConfigSO","ConveyorSpawnPointConfigSO","ConveyorSpeedBoostConfigSO","CarrierSystem","CarrierSpawner","CarrierConfigSO","CapacityManager","GameConditionManager","GameConditionConfigSO","LevelManager","LevelData","LevelEntryAnimConfigSO","CameraManager","InputController","PoolManagerNew","ConfigManager","ColorConfigSO","CatColorConfigSO","AnimBlockConfig","StylizedColorConfigSO","RemainingColorConfigSO","CustomTimeScaleGroup","DG.Tweening.Core.DOTweenSettings","UnityEngine.AudioLowPassFilter","UnityEngine.AudioBehaviour","UnityEngine.AudioHighPassFilter","UnityEngine.AudioReverbFilter","UnityEngine.AudioDistortionFilter","UnityEngine.AudioEchoFilter","UnityEngine.AudioChorusFilter","UnityEngine.Cloth","UnityEngine.FlareLayer","UnityEngine.ConstantForce","UnityEngine.Joint","UnityEngine.HingeJoint","UnityEngine.SpringJoint","UnityEngine.FixedJoint","UnityEngine.CharacterJoint","UnityEngine.ConfigurableJoint","UnityEngine.CompositeCollider2D","UnityEngine.Rigidbody2D","UnityEngine.Joint2D","UnityEngine.AnchoredJoint2D","UnityEngine.SpringJoint2D","UnityEngine.DistanceJoint2D","UnityEngine.FrictionJoint2D","UnityEngine.HingeJoint2D","UnityEngine.RelativeJoint2D","UnityEngine.SliderJoint2D","UnityEngine.TargetJoint2D","UnityEngine.FixedJoint2D","UnityEngine.WheelJoint2D","UnityEngine.ConstantForce2D","UnityEngine.StreamingController","UnityEngine.TextMesh","UnityEngine.Tilemaps.TilemapRenderer","UnityEngine.Tilemaps.Tilemap","UnityEngine.Tilemaps.TilemapCollider2D","UnityEngine.Splines.SplineExtrude","TMPro.TextContainer","TMPro.TextMeshPro","TMPro.TextMeshProUGUI","TMPro.TMP_Dropdown","TMPro.TMP_SelectionCaret","TMPro.TMP_SubMesh","TMPro.TMP_SubMeshUI","TMPro.TMP_Text","UnityEngine.UI.Dropdown","UnityEngine.UI.Graphic","UnityEngine.UI.GraphicRaycaster","UnityEngine.UI.AspectRatioFitter","UnityEngine.UI.ContentSizeFitter","UnityEngine.UI.GridLayoutGroup","UnityEngine.UI.HorizontalLayoutGroup","UnityEngine.UI.HorizontalOrVerticalLayoutGroup","UnityEngine.UI.LayoutElement","UnityEngine.UI.LayoutGroup","UnityEngine.UI.VerticalLayoutGroup","UnityEngine.UI.Mask","UnityEngine.UI.MaskableGraphic","UnityEngine.UI.RawImage","UnityEngine.UI.RectMask2D","UnityEngine.UI.Scrollbar","UnityEngine.UI.ScrollRect","UnityEngine.UI.Slider","UnityEngine.UI.Text","UnityEngine.UI.Toggle","UnityEngine.EventSystems.BaseInputModule","UnityEngine.EventSystems.EventSystem","UnityEngine.EventSystems.PointerInputModule","UnityEngine.EventSystems.StandaloneInputModule","UnityEngine.EventSystems.TouchInputModule","UnityEngine.EventSystems.Physics2DRaycaster","UnityEngine.EventSystems.PhysicsRaycaster"]

Deserializers.unityVersion = "2022.3.62f3";

Deserializers.productName = "Loop sort PLA";

Deserializers.lunaInitializationTime = "07/09/2026 07:54:59";

Deserializers.lunaDaysRunning = "1.1";

Deserializers.lunaVersion = "7.2.0";

Deserializers.lunaSHA = "ea08d29afe2968efcb8d91d5624f033c6485cc68";

Deserializers.creativeName = "";

Deserializers.lunaAppID = "0";

Deserializers.projectId = "dc2b0d14efb857f4ea56325f3c38e974";

Deserializers.packagesInfo = "com.unity.textmeshpro: 3.0.6\ncom.unity.ugui: 1.0.0";

Deserializers.externalJsLibraries = "";

Deserializers.androidLink = ( typeof window !== "undefined")&&window.$environment.packageConfig.androidLink?window.$environment.packageConfig.androidLink:'Empty';

Deserializers.iosLink = ( typeof window !== "undefined")&&window.$environment.packageConfig.iosLink?window.$environment.packageConfig.iosLink:'Empty';

Deserializers.base64Enabled = "True";

Deserializers.minifyEnabled = "True";

Deserializers.isForceUncompressed = "False";

Deserializers.isAntiAliasingEnabled = "False";

Deserializers.isRuntimeAnalysisEnabledForCode = "False";

Deserializers.runtimeAnalysisExcludedClassesCount = "1766";

Deserializers.runtimeAnalysisExcludedMethodsCount = "5095";

Deserializers.runtimeAnalysisExcludedModules = "physics2d";

Deserializers.isRuntimeAnalysisEnabledForShaders = "True";

Deserializers.isRealtimeShadowsEnabled = "False";

Deserializers.isLunaCompilerV2Used = "True";

Deserializers.companyName = "DefaultCompany";

Deserializers.buildPlatform = "Android";

Deserializers.applicationIdentifier = "com.DefaultCompany.LoopsortPLA";

Deserializers.disableAntiAliasing = true;

Deserializers.graphicsConstraint = 24;

Deserializers.linearColorSpace = true;

Deserializers.buildID = "2289538d-fc42-44d7-af7d-b50515f04238";

Deserializers.runtimeInitializeOnLoadInfos = [[["UnityEngine","Experimental","Rendering","ScriptableRuntimeReflectionSystemSettings","ScriptingDirtyReflectionSystemInstance"]],[],[],[],[]];

Deserializers.typeNameToIdMap = function(){ var i = 0; return Deserializers.types.reduce( function( res, item ) { res[ item ] = i++; return res; }, {} ) }()

