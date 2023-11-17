using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (blockSel != null)
            {
                if (IsChoppable(byEntity.World.BlockAccessor.GetBlock(blockSel.Position)))
                {
                    byPlayer.Entity.StartAnimation("axechop");
                    handHandling = EnumHandHandling.PreventDefault;
                    byEntity.WatchedAttributes.SetBool("didchop", false);
                    byEntity.WatchedAttributes.SetBool("haschoppedblock", false);
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.Handled;
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
            Block block = blockAccessor.GetBlock(blockSel.Position);

            if (blockSel != null)
            {
                if (IsChoppable(block))
                {
                    if (byEntity.World.Side == EnumAppSide.Client)
                    {
                        AnimationStep(secondsUsed, byEntity);

                        if (secondsUsed > 0.4f && byEntity.WatchedAttributes.GetBool("didchop") == false)
                        {
                            var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
                           
                            byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/block/chop"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)byEntity.World.Rand.NextDouble() * 0.2f, 16, 1f);

                            (byEntity.World as IClientWorldAccessor)?.AddCameraShake(0.25f);
                            byEntity.WatchedAttributes.SetBool("didchop", true);
                            byEntity.WatchedAttributes.SetBool("haschoppedblock", true);
                            //Crappy way to fix animation bug T_T
                            TryStopAnimation(byEntity, byPlayer);
                        }
                    }


                    if (byEntity.World.Side == EnumAppSide.Server)
                    {
                        if (secondsUsed > 0.45f && !byEntity.WatchedAttributes.GetBool("haschoppedblock", false))
                        {
                            int minToolTier = byEntity.World.Config.GetInt("intsaChopMinTier");

                            Item item = byEntity.RightHandItemSlot.Itemstack.Item;
                            float chopChance = item.ToolTier / (float)(minToolTier == 0 ? 1 : minToolTier);
                            if (byEntity.World.Rand.NextDouble() > chopChance)
                            {
                                blockAccessor.DamageBlock(blockSel.Position, BlockFacing.FromNormal(byEntity.Pos.GetViewVector()), block.Resistance * chopChance);
                                if (byEntity.World.Config.TryGetBool("damageToolOnChop") == true)
                                {
                                    item.DamageItem(byEntity.World, byEntity, byEntity.RightHandItemSlot);
                                }
                            }
                            else
                            {
                                BlockBehaviorAxeChoppable blockBehavior = block.GetBehavior(typeof(BlockBehaviorAxeChoppable), true) as BlockBehaviorAxeChoppable;

                                blockAccessor.BreakBlock(blockSel.Position, byPlayer, 0);
                                blockAccessor.MarkBlockDirty(blockSel.Position, byPlayer);
                                Item drops = byEntity.Api.World.GetItem(blockBehavior.drop);

                                for (int i = 0; i < blockBehavior.dropAmount; i++)
                                {
                                    byEntity.Api.World.SpawnItemEntity(new ItemStack(drops, 1), blockSel.Position.ToVec3d());
                                }

                                item.DamageItem(byEntity.World, byEntity, byEntity.RightHandItemSlot);

                                if (byEntity.World.Config.TryGetBool("AutoLogPlacement") == true)
                                {
                                    PlaceNextLog(byEntity, byPlayer, blockAccessor, blockSel);
                                }
                            }
                            byEntity.WatchedAttributes.SetBool("haschoppedblock", true);


                        }
                    }
                }
            }
            return secondsUsed < 0.75f;
        }

        private void TryStopAnimation(EntityAgent byEntity, IPlayer byPlayer)
        {
            if (byEntity.RightHandItemSlot.Itemstack.Item.GetRemainingDurability(byEntity.RightHandItemSlot.Itemstack) <= 1)
            {
                byPlayer.Entity.StopAnimation("axechop");
            }   
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            byPlayer.Entity.StopAnimation("axechop");
            handled = EnumHandling.PreventDefault;
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            byPlayer.Entity.StopAnimation("axechop");

        }
        private void PlaceNextLog(EntityAgent byEntity, IPlayer byPlayer, IBlockAccessor blockAccessor, BlockSelection blockSel)
        {
            Block foungLog = null;
            ItemSlot logInSlot = null;
            byEntity.WalkInventory((currentSlot) =>
            {
                if (currentSlot is ItemSlotCreative) return true;
                if (!(currentSlot.Inventory is InventoryBasePlayer)) return true;
                Block blockInCurrentSlot = currentSlot.Itemstack?.Block;
                if (blockInCurrentSlot != null && IsChoppable(blockInCurrentSlot))
                {
                    foungLog = blockInCurrentSlot;
                    logInSlot = currentSlot;
                    return false;
                }
                return true;
            });
            if (foungLog != null)
            {
                blockAccessor.SetBlock(foungLog.Id, blockSel.Position);
                byEntity.World.PlaySoundAt(foungLog.Sounds.Place, byPlayer);
                logInSlot.TakeOut(1);
                logInSlot.MarkDirty();
            }
        }




        private bool IsChoppable(Block block)
        {
            return block.HasBehavior<BlockBehaviorAxeChoppable>();
        }

        private void AnimationStep(float secondsUsed, EntityAgent byEntity)
        {
            float t = secondsUsed * 1f;
            float backwards = -Math.Min(0.35f, 2 * t);
            float stab = Math.Min(1.2f, 20 * Math.Max(0, t - 0.35f));


            var tf = new ModelTransform();
            tf.EnsureDefaultValues();

            float sum = stab + backwards;
            float easeout = Math.Max(0, 2 * (t - 1));

            if (t > 0.4f) sum = Math.Max(0, sum - easeout);

            tf.Translation.Set(-1.4f * sum, sum, -sum * 0.8f * 2.6f);
            tf.Rotation.Set(-sum * 45, 0, sum * 10);

            byEntity.Controls.UsingHeldItemTransformAfter = tf;

        }


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
    }
}