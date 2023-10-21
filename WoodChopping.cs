using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveWoodchopping
{
    public class WoodChopping : CollectibleBehavior
    {
        public WoodChopping(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            IPlayer byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID); 
            if (blockSel != null)
            {
                if (IsChoppable(byEntity.World.BlockAccessor.GetBlock(blockSel.Position), byEntity))
                {
                    byPlayer.Entity.StartAnimation("axechop");
                    handHandling = EnumHandHandling.PreventDefault;
                    if (byEntity.World.Side == EnumAppSide.Server)
                    {
                        byEntity.WatchedAttributes.SetBool("didchop", false);
                        byEntity.WatchedAttributes.SetBool("haschoppedblock", false);
                    }
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
            handling = EnumHandling.PreventDefault;

            float t = secondsUsed * 1f;

            float backwards = -Math.Min(0.35f, 2 * t);
            float stab = Math.Min(1.2f, 20 * Math.Max(0, t - 0.35f));
            IPlayer byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);



            if (blockSel != null)
            {
                if (IsChoppable(byEntity.World.BlockAccessor.GetBlock(blockSel.Position), byEntity))
                {
                    if (byEntity.World.Side == EnumAppSide.Client)
                    {

                        ModelTransform tf = new ModelTransform();
                        tf.EnsureDefaultValues();

                        float sum = stab + backwards;
                        float easeout = Math.Max(0, 2 * (t - 1));

                        if (t > 0.4f) sum = Math.Max(0, sum - easeout);

                        tf.Translation.Set(-1.4f * sum, sum, -sum * 0.8f * 2.6f);
                        tf.Rotation.Set(-sum * 45, 0, sum * 10);

                        byEntity.Controls.UsingHeldItemTransformAfter = tf;


                        if (secondsUsed > 0.4f && byEntity.WatchedAttributes.GetBool("didchop") == false)
                        {
                            if (IsChoppable(byEntity.World.BlockAccessor.GetBlock(blockSel.Position), byEntity))
                            {
                                handling = EnumHandling.PreventDefault;
                                var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
                                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/block/chop"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)byEntity.World.Rand.NextDouble() * 0.2f, 16, 1f);
                                (byEntity.World as IClientWorldAccessor)?.AddCameraShake(0.25f);
                                byEntity.WatchedAttributes.SetBool("didchop", true);
                                byEntity.WatchedAttributes.SetBool("haschoppedblock", true);
                            }
                        }

                    }
                    if (byEntity.World.Side == EnumAppSide.Server)
                    {
                        if (secondsUsed > 0.45f && !byEntity.WatchedAttributes.GetBool("haschoppedblock", false))
                        {
                            BlockBehaviorAxeChoppable behavior = byEntity.World.BlockAccessor.GetBlock(blockSel.Position)
                                .GetBehavior(typeof (BlockBehaviorAxeChoppable), true) as BlockBehaviorAxeChoppable;
                            /*CraftingRecipeIngredient firewoodResult = null;

                            foreach (var key in ImmersiveWoodchoppingModSystem.choppingRecipes.Keys)
                            {
                                if (WildcardUtil.Match(key, new AssetLocation(GetBlockPath(blockSel, byEntity)).ToString()))
                                {
                                    firewoodResult = ImmersiveWoodchoppingModSystem.choppingRecipes[key];
                                }
                            }*/
                            byEntity.Api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, 0);
                            byEntity.Api.World.BlockAccessor.MarkBlockDirty(blockSel.Position, byPlayer);
                            Item drops = byEntity.Api.World.GetItem(behavior.drop);
                            for (int i = 0; i < behavior.dropAmount; i++)
                            {
                                byEntity.Api.World.SpawnItemEntity(new ItemStack(drops, 1), blockSel.Position.ToVec3d());
                            }
                            byEntity.WatchedAttributes.SetBool("haschoppedblock", true);
                            byEntity.RightHandItemSlot.Itemstack.Item.DamageItem(byEntity.World, byEntity, byEntity.RightHandItemSlot);
                        }
                    }
                }
            }
            return secondsUsed < 0.75f;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        {
            base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
            IPlayer byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            byPlayer.Entity.StopAnimation("axechop");
            handled = EnumHandling.PreventDefault;
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
            IPlayer byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            byPlayer.Entity.StopAnimation("axechop");
        }


        
        private bool IsChoppable(Block block, EntityAgent byEntity)
        {
            if (block.HasBehavior<BlockBehaviorAxeChoppable>())
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //(IsChoppable(byEntity.World.BlockAccessor.GetBlock(blockSel.Position),byEntity))
        //(IsPlacedLog(GetBlockPath(blockSel, byEntity)))


        //Old method of checking for valid log
        /* 
        private bool IsPlacedLog(string path)
        {
            return path.StartsWith("log-placed") || path.StartsWith("logsection-placed");
        }
        */
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "immersivewoodchopping:heldhelp-chop",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
        public string GetBlockPath(BlockSelection blockSel, EntityAgent byEntity)
        {
            return byEntity.World.BlockAccessor.GetBlock(blockSel.Position).Code.Path;
        }

    }
}